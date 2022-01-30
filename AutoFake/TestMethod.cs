using System;
using System.Collections.Generic;
using System.Linq;
using AutoFake.Abstractions;
using AutoFake.Abstractions.Setup.Mocks;
using AutoFake.Setup.Mocks;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake
{
	/// <summary>Specifies that when a method returns <see cref="ReturnValue"/>, the parameter will not be null even if the corresponding type allows it.</summary>
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
	public sealed class NotNullWhenAttribute : Attribute
	{
		/// <summary>Initializes the attribute with the specified return value condition.</summary>
		/// <param name="returnValue">
		/// The return value condition. If the method returns this value, the associated parameter will not be null.
		/// </param>
		public NotNullWhenAttribute(bool returnValue) => ReturnValue = returnValue;

		/// <summary>Gets the return value condition.</summary>
		public bool ReturnValue { get; }
	}

	internal class TestMethod
	{
		private readonly IEmitterPool _emitterPool;
		private readonly ITypeInfo _typeInfo;
		private readonly FakeOptions _options;
		private readonly IGenericArgumentProcessor _genericArgumentProcessor;
		private readonly IAssemblyWriter _assemblyWriter;

		public TestMethod(IEmitterPool emitterPool, ITypeInfo typeInfo, FakeOptions fakeOptions,
			IGenericArgumentProcessor genericArgumentProcessor, IAssemblyWriter assemblyWriter)
		{
			_emitterPool = emitterPool;
			_typeInfo = typeInfo;
			_options = fakeOptions;
			_genericArgumentProcessor = genericArgumentProcessor;
			_assemblyWriter = assemblyWriter;
		}

		public IReadOnlyList<MethodDefinition> Rewrite(MethodDefinition originalMethod, IEnumerable<IMock> mocks,
			IEnumerable<GenericArgument> genericArgs)
		{
			var state = new State(originalMethod, genericArgs);
			Rewrite(mocks, originalMethod, state);
			return state.Methods.ToReadOnlyList();
		}

		private void Rewrite(IEnumerable<IMock> mocks, MethodDefinition? currentMethod, State state)
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

			if (currentMethod.IsAsync(out var asyncMethod)) Rewrite(mocks, asyncMethod, UpdateParents(state, currentMethod));
			if (currentMethod.Body != null) ProcessInstructions(mocks, currentMethod, state);
		}

		private bool Validate(MethodDefinition currentMethod, State state)
		{
			return !CheckAnalysisLevel(currentMethod, state) || !CheckVirtualMember(currentMethod) ||
			       !state.MethodContracts.Add(currentMethod.ToString());
		}

		private void ProcessInstructions(IEnumerable<IMock> mocks, MethodDefinition currentMethod, State state)
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

		private void ProcessMocks(MethodDefinition currentMethod, Instruction instruction, IEnumerable<IMock> mocks, State state)
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
			state.Parents = state.Parents.Concat(new[] {currentMethod});
			return state;
		}

		private bool CheckAnalysisLevel(MethodReference methodRef, State state)
		{
			switch (_options.AnalysisLevel)
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

		private class State
		{
			public State(MethodDefinition originalMethod, IEnumerable<GenericArgument> genericArgs)
			{
				OriginalMethod = originalMethod;
				GenericArgs = genericArgs;
				Parents = Enumerable.Empty<MethodDefinition>();
				Methods = new List<MethodDefinition>();
				MethodContracts = new();
				Implementations = new();
				IsOriginalInstruction = true;
			}

			public MethodDefinition OriginalMethod { get; }
			public IEnumerable<GenericArgument> GenericArgs { get; set; }
			public IEnumerable<MethodDefinition> Parents { get; set; }
			public IList<MethodDefinition> Methods { get; }
			public HashSet<string> MethodContracts { get; }
			public HashSet<MethodDefinition> Implementations { get; }
			public bool IsOriginalInstruction { get; set; }
		}
	}
}
