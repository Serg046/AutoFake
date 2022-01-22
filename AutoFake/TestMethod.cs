using System;
using System.Collections.Generic;
using System.Linq;
using AutoFake.Setup.Mocks;
using Mono.Cecil;

namespace AutoFake
{
	internal class TestMethod
	{
		private readonly MethodDefinition _originalMethod;
		private readonly IEmitterPool _emitterPool;
		private readonly ITypeInfo _typeInfo;
		private readonly FakeOptions _options;
		private readonly IContractProcessor _contractProcessor;
		private readonly IAssemblyWriter _assemblyWriter;
		private readonly TestMethodInstructionProcessor.Create _createTestMethodInstructionProcessor;
		private readonly HashSet<string> _methodContracts;
		private readonly List<MethodDefinition> _methods;
		private readonly HashSet<MethodDefinition> _implementations;

		public TestMethod(MethodDefinition originalMethod, IEmitterPool emitterPool, ITypeInfo typeInfo, FakeOptions fakeOptions,
			IContractProcessor contractProcessor, IAssemblyWriter assemblyWriter, TestMethodInstructionProcessor.Create createTestMethodInstructionProcessor)
		{
			_originalMethod = originalMethod;
			_emitterPool = emitterPool;
			_typeInfo = typeInfo;
			_options = fakeOptions;
			_contractProcessor = contractProcessor;
			_assemblyWriter = assemblyWriter;
			_createTestMethodInstructionProcessor = createTestMethodInstructionProcessor;
			_methodContracts = new HashSet<string>();
			_methods = new List<MethodDefinition>();
			_implementations = new HashSet<MethodDefinition>();
		}

		public void RewriteAndProcessContracts(IEnumerable<IMock> mocks, IEnumerable<GenericArgument> genericArgs, HashSet<IMock> replaceContractMocks)
		{
			Rewrite(mocks, genericArgs);
			_contractProcessor.ProcessAllOriginalMethodContractsWithMocks(_originalMethod, replaceContractMocks);
			foreach (var methodDef in _methods.Where(m => m != _originalMethod))
			{
				_contractProcessor.ProcessOriginalMethodContract(methodDef);
			}
		}

		public void Rewrite(IEnumerable<IMock> mocks, IEnumerable<GenericArgument> genericArgs)
		{
			Rewrite(mocks, _originalMethod, genericArgs);
		}

		private void Rewrite(IEnumerable<IMock> mocks, MethodDefinition methodDef, IEnumerable<GenericArgument> genericArgs)
		{
			_methods.Clear();
			_methodContracts.Clear();
			_implementations.Clear();
			Rewrite(mocks, methodDef, Enumerable.Empty<MethodDefinition>(), genericArgs);
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
				var proc = _createTestMethodInstructionProcessor(_originalMethod, _emitterPool, mocks, parents, genericArgs);
				proc.Process(currentMethod, instruction, Rewrite);
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
