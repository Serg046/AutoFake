using System;
using System.Collections.Generic;
using System.Linq;
using AutoFake.Abstractions;
using AutoFake.Abstractions.Setup.Mocks;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake
{
	internal class TestMethod
	{
		private readonly IEmitterPool _emitterPool;
		private readonly ITypeInfo _typeInfo;
		private readonly IGenericArgumentProcessor _genericArgumentProcessor;
		private readonly IAssemblyWriter _assemblyWriter;
		private readonly Func<string, string, string, string[], IMethodContract> _createMethodContract;

		public TestMethod(IEmitterPool emitterPool, ITypeInfo typeInfo,
			IGenericArgumentProcessor genericArgumentProcessor, IAssemblyWriter assemblyWriter,
			Func<string, string, string, string[], IMethodContract> createMethodContract)
		{
			_emitterPool = emitterPool;
			_typeInfo = typeInfo;
			_genericArgumentProcessor = genericArgumentProcessor;
			_assemblyWriter = assemblyWriter;
			_createMethodContract = createMethodContract;
		}

		public IReadOnlyList<MethodDefinition> Rewrite(MethodDefinition originalMethod, IFakeOptions options,
			IEnumerable<IMockInjector> mocks, IEnumerable<GenericArgument> genericArgs)
		{
			var state = new State(originalMethod, genericArgs, options);
			Rewrite(mocks, originalMethod, state);
			return state.Methods.ToReadOnlyList();
		}

		private void Rewrite(IEnumerable<IMockInjector> mocks, MethodDefinition? currentMethod, State state)
		{
			if (currentMethod == null || Validate(currentMethod, state)) return;
			state.Methods.Add(currentMethod);

			if ((currentMethod.DeclaringType.IsInterface || currentMethod.IsVirtual) && !state.Implementations.Contains(currentMethod))
			{
				var implementations = _typeInfo.GetAllImplementations(currentMethod, includeAffectedAssemblies: true);
				foreach (var implementation in implementations)
				{
					state.Implementations.Add(implementation);
				}

				foreach (var methodDef in implementations)
				{
					Rewrite(mocks, methodDef, UpdateParents(state, currentMethod));
				}
			}

			foreach (var method in currentMethod.GetStateMachineMethods(methods => methods.Single(m => m.Name == "MoveNext")))
			{
				Rewrite(mocks, method, UpdateParents(state, currentMethod));
			}
			if (currentMethod.Body != null) ProcessInstructions(mocks, currentMethod, state);
		}

		private bool Validate(MethodDefinition currentMethod, State state)
		{
			return !CheckAnalysisLevel(currentMethod, state) || !CheckVirtualMember(currentMethod, state) ||
				   !state.MethodContracts.Add(currentMethod.ToString());
		}

		private void ProcessInstructions(IEnumerable<IMockInjector> mocks, MethodDefinition currentMethod, State state)
		{
			foreach (var instruction in currentMethod.Body.Instructions.ToList())
			{
				if (instruction.Operand is MethodReference method)
				{
					var methodDefinition = method.ToMethodDefinition();
					state.GenericArgs = _genericArgumentProcessor.GetGenericArguments(method, methodDefinition).Concat(state.GenericArgs);
					Rewrite(mocks, methodDefinition, UpdateParents(state, currentMethod));
				}
				else if (instruction.Operand is FieldReference field)
				{
					state.GenericArgs = _genericArgumentProcessor.GetGenericArguments(field).Concat(state.GenericArgs);
				}

				ProcessMocks(currentMethod, instruction, mocks, state);
			}
		}

		private void ProcessMocks(MethodDefinition currentMethod, Instruction instruction, IEnumerable<IMockInjector> mocks, State state)
		{
			foreach (var mock in mocks)
			{
				if (mock.IsSourceInstruction(state.OriginalMethod, instruction, state.GenericArgs))
				{
					var emitter = _emitterPool.GetEmitter(currentMethod.Body);
					if (state.IsOriginalInstruction)
					{
						instruction = emitter.ShiftDown(instruction);
						state.IsOriginalInstruction = false;
					}

					mock.Inject(emitter, instruction);
					TryAddAffectedAssembly(currentMethod, state);
					foreach (var parent in state.Parents)
					{
						TryAddAffectedAssembly(parent, state);
					}
				}
			}
		}

		private void TryAddAffectedAssembly(MethodDefinition currentMethod, State state)
		{
			if (currentMethod.Module.Assembly != state.OriginalMethod.Module.Assembly)
			{
				_assemblyWriter.TryAddAffectedAssembly(currentMethod.Module.Assembly);
			}
		}

		private State UpdateParents(State state, MethodDefinition currentMethod)
		{
			state.Parents = state.Parents.Concat(new[] { currentMethod });
			return state;
		}

		private bool CheckAnalysisLevel(MethodReference methodRef, State state)
		{
			switch (state.Options.AnalysisLevel)
			{
				case AnalysisLevels.Type:
					{
						if (methodRef.DeclaringType.FullName == state.OriginalMethod.DeclaringType.FullName &&
							methodRef.Module.Assembly == state.OriginalMethod.Module.Assembly) return true;
						break;
					}
				case AnalysisLevels.Assembly:
					{
						if (methodRef.Module.Assembly == state.OriginalMethod.Module.Assembly) return true;
						break;
					}
				case AnalysisLevels.AllExceptSystemAndMicrosoft:
					{
						return !methodRef.DeclaringType.Namespace.StartsWith(nameof(System)) &&
							   !methodRef.DeclaringType.Namespace.StartsWith(nameof(Microsoft));
					}
				default: throw new NotSupportedException($"{state.Options.AnalysisLevel} is not supported");
			}

			return _typeInfo.IsInReferencedAssembly(methodRef.DeclaringType.Module.Assembly);
		}

		private bool CheckVirtualMember(MethodDefinition method, State state)
		{
			if (!method.IsVirtual) return true;
			if (state.Options.DisableVirtualMembers) return false;

			var contract = _createMethodContract(method.DeclaringType.ToString(), method.ReturnType.ToString(),
				method.Name, method.Parameters.Select(p => p.ParameterType.ToString()).ToArray());
			return state.Options.AllowedVirtualMembers.Count == 0 || state.Options.AllowedVirtualMembers.Any(m => m(contract));
		}

		private class State
		{
			public State(MethodDefinition originalMethod, IEnumerable<GenericArgument> genericArgs, IFakeOptions options)
			{
				OriginalMethod = originalMethod;
				GenericArgs = genericArgs;
				Options = options;
				Parents = Enumerable.Empty<MethodDefinition>();
				Methods = new List<MethodDefinition>();
				MethodContracts = new();
				Implementations = new();
				IsOriginalInstruction = true;
			}

			public MethodDefinition OriginalMethod { get; }
			public IEnumerable<GenericArgument> GenericArgs { get; set; }
			public IFakeOptions Options { get; }
			public IEnumerable<MethodDefinition> Parents { get; set; }
			public IList<MethodDefinition> Methods { get; }
			public HashSet<string> MethodContracts { get; }
			public HashSet<MethodDefinition> Implementations { get; }
			public bool IsOriginalInstruction { get; set; }
		}
	}
}
