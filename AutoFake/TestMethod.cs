using System;
using System.Collections.Generic;
using System.Linq;
using AutoFake.Setup.Mocks;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake
{
	internal class TestMethod
	{
		private readonly MethodDefinition _originalMethod;
		private readonly IEmitterPool _emitterPool;
		private readonly ITypeInfo _typeInfo;
		private readonly FakeOptions _options;
		private readonly IGenericArgumentProcessor _genericArgumentProcessor;
		private readonly IAssemblyWriter _assemblyWriter;
		private readonly HashSet<string> _methodContracts;
		private readonly List<MethodDefinition> _methods;
		private readonly HashSet<MethodDefinition> _implementations;
		private bool _isOriginalInstruction;

		public TestMethod(MethodDefinition originalMethod, IEmitterPool emitterPool, ITypeInfo typeInfo, FakeOptions fakeOptions,
			IGenericArgumentProcessor genericArgumentProcessor, IAssemblyWriter assemblyWriter)
		{
			_originalMethod = originalMethod;
			_emitterPool = emitterPool;
			_typeInfo = typeInfo;
			_options = fakeOptions;
			_genericArgumentProcessor = genericArgumentProcessor;
			_assemblyWriter = assemblyWriter;
			_methodContracts = new HashSet<string>();
			_methods = new List<MethodDefinition>();
			_implementations = new HashSet<MethodDefinition>();
			_isOriginalInstruction = true;
		}
		
		public IReadOnlyList<MethodDefinition> Rewrite(IEnumerable<IMock> mocks, IEnumerable<GenericArgument> genericArgs)
		{
			_methods.Clear();
			_methodContracts.Clear();
			_implementations.Clear();
			Rewrite(mocks, _originalMethod, Enumerable.Empty<MethodDefinition>(), genericArgs);
			return _methods.ToReadOnlyList();
		}

		private void Rewrite(IEnumerable<IMock> mocks, MethodDefinition? currentMethod, IEnumerable<MethodDefinition> parents, IEnumerable<GenericArgument> genericArgs)
		{
			if (currentMethod == null || !CheckAnalysisLevel(currentMethod) || !CheckVirtualMember(currentMethod) || !_methodContracts.Add(currentMethod.ToString())) return;
			_methods.Add(currentMethod);

			if ((currentMethod.DeclaringType.IsInterface || currentMethod.IsVirtual) && !_implementations.Contains(currentMethod))
			{
				var implementations = _typeInfo.GetAllImplementations(currentMethod, includeAffectedAssemblies: true);
				foreach (var implementation in implementations)
				{
					_implementations.Add(implementation);
				}

				foreach (var methodDef in implementations)
				{
					Rewrite(mocks, methodDef, GetParents(parents, currentMethod), genericArgs);
				}
			}

			if (currentMethod.IsAsync(out var asyncMethod))
			{
				Rewrite(mocks, asyncMethod, GetParents(parents, currentMethod), genericArgs);
			}

			if (currentMethod.Body != null)
			{
				ProcessInstructions(mocks, currentMethod, parents, genericArgs);
			}
		}

		private void ProcessInstructions(IEnumerable<IMock> mocks, MethodDefinition currentMethod, IEnumerable<MethodDefinition> parents, IEnumerable<GenericArgument> genericArgs)
		{
			foreach (var instruction in currentMethod.Body.Instructions.ToList())
			{
				if (instruction.Operand is MethodReference method)
				{
					var methodDefinition = method.ToMethodDefinition();
					genericArgs = _genericArgumentProcessor.GetGenericArguments(method, methodDefinition).Concat(genericArgs);
					Rewrite(mocks, methodDefinition, parents.Concat(new[] { currentMethod }), genericArgs);
				}
				else if (instruction.Operand is FieldReference field)
				{
					genericArgs = _genericArgumentProcessor.GetGenericArguments(field).Concat(genericArgs);
				}

				ProcessMocks(currentMethod, instruction, mocks, parents, genericArgs);
			}
		}

		private void ProcessMocks(MethodDefinition currentMethod, Instruction instruction,
			IEnumerable<IMock> mocks, IEnumerable<MethodDefinition> parents, IEnumerable<GenericArgument> genericArgs)
		{
			foreach (var mock in mocks)
			{
				if (mock.IsSourceInstruction(_originalMethod, instruction, genericArgs))
				{
					var emitter = _emitterPool.GetEmitter(currentMethod.Body);
					if (_isOriginalInstruction)
					{
						instruction = emitter.ShiftDown(instruction);
						_isOriginalInstruction = false;
					}

					mock.Inject(emitter, instruction);
					TryAddAffectedAssembly(currentMethod);
					foreach (var parent in parents)
					{
						TryAddAffectedAssembly(parent);
					}
				}
			}
		}

		private void TryAddAffectedAssembly(MethodDefinition currentMethod)
		{
			if (currentMethod.Module.Assembly != _originalMethod.Module.Assembly)
			{
				_assemblyWriter.TryAddAffectedAssembly(currentMethod.Module.Assembly);
			}
		}

		private IEnumerable<MethodDefinition> GetParents(IEnumerable<MethodDefinition> parents, MethodDefinition currentMethod) => parents.Concat(new[] { currentMethod });

		private bool CheckAnalysisLevel(MethodReference methodRef)
		{
			switch (_options.AnalysisLevel)
			{
				case AnalysisLevels.Type:
					{
						if (methodRef.DeclaringType.FullName == _originalMethod.DeclaringType.FullName &&
							methodRef.Module.Assembly == _originalMethod.Module.Assembly) return true;
						break;
					}
				case AnalysisLevels.Assembly:
					{
						if (methodRef.Module.Assembly == _originalMethod.Module.Assembly) return true;
						break;
					}
				case AnalysisLevels.AllExceptSystemAndMicrosoft:
					{
						return !methodRef.DeclaringType.Namespace.StartsWith(nameof(System)) &&
							   !methodRef.DeclaringType.Namespace.StartsWith(nameof(Microsoft));
					}
				default: throw new NotSupportedException($"{_options.AnalysisLevel} is not supported");
			}

			return _typeInfo.IsInReferencedAssembly(methodRef.DeclaringType.Module.Assembly);
		}

		private bool CheckVirtualMember(MethodDefinition method)
		{
			if (!method.IsVirtual) return true;
			if (_options.DisableVirtualMembers) return false;
			var contract = method.ToMethodContract();
			return _options.AllowedVirtualMembers.Count == 0 || _options.AllowedVirtualMembers.Any(m => m(contract));
		}
	}
}
