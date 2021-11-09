using System;
using System.Collections.Generic;
using System.Linq;
using AutoFake.Setup.Mocks;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake
{
	internal class TestMethodInstructionProcessor
	{
		private readonly MethodDefinition _originalMethod;
		private readonly IEmitterPool _emitterPool;
		private readonly IAssemblyWriter _assemblyWriter;
		private readonly IEnumerable<IMock> _mocks;
		private readonly IEnumerable<MethodDefinition> _parents;
		private IEnumerable<GenericArgument> _genericArgs;
		private bool _isOriginalInstruction;

		public TestMethodInstructionProcessor(MethodDefinition originalMethod, IEmitterPool emitterPool, IAssemblyWriter assemblyWriter, IEnumerable<IMock> mocks, IEnumerable<MethodDefinition> parents, IEnumerable<GenericArgument> genericArgs)
		{
			_originalMethod = originalMethod;
			_emitterPool = emitterPool;
			_assemblyWriter = assemblyWriter;
			_mocks = mocks;
			_parents = parents;
			_genericArgs = genericArgs;
			_isOriginalInstruction = true;
		}

		public void Process(MethodDefinition currentMethod, Instruction instruction,
			Action<IEnumerable<IMock>, MethodDefinition?, IEnumerable<MethodDefinition>, IEnumerable<GenericArgument>> rewrite)
		{
			if (instruction.Operand is MethodReference method)
			{
				var methodDefinition = method.ToMethodDefinition();
				_genericArgs = GetGenericArguments(method, methodDefinition).Concat(_genericArgs);
				rewrite(_mocks, methodDefinition, _parents.Concat(new[] { currentMethod }), _genericArgs);
			}
			else if (instruction.Operand is FieldReference field)
			{
				_genericArgs = GetGenericArguments(field).Concat(_genericArgs);
			}

			ProcessMocks(currentMethod, instruction);
		}

		private void ProcessMocks(MethodDefinition currentMethod, Instruction instruction)
		{
			foreach (var mock in _mocks)
			{
				if (mock.IsSourceInstruction(_originalMethod, instruction, _genericArgs))
				{
					var emitter = _emitterPool.GetEmitter(currentMethod.Body);
					if (_isOriginalInstruction)
					{
						instruction = emitter.ShiftDown(instruction);
						_isOriginalInstruction = false;
					}

					mock.Inject(emitter, instruction);
					TryAddAffectedAssembly(currentMethod);
					foreach (var parent in _parents)
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

		private IEnumerable<GenericArgument> GetGenericArguments(MethodReference methodRef, MethodDefinition methodDef)
		{
			if (methodRef is GenericInstanceMethod genericInstanceMethod)
			{
				for (var i = 0; i < genericInstanceMethod.GenericArguments.Count; i++)
				{
					var genericArgument = genericInstanceMethod.GenericArguments[i];
					var declaringType = methodDef.DeclaringType.ToString();
					yield return new GenericArgument(
						methodDef.GenericParameters[i].Name,
						genericArgument.ToString(),
						declaringType,
						GetGenericDeclaringType(genericArgument as GenericParameter));
				}
			}

			foreach (var arg in GetGenericArguments(methodRef.DeclaringType, methodDef.DeclaringType))
			{
				yield return arg;
			}
		}

		private IEnumerable<GenericArgument> GetGenericArguments(TypeReference typeRef, TypeDefinition typeDef)
		{
			if (typeRef is GenericInstanceType genericInstanceType)
			{
				for (var i = 0; i < genericInstanceType.GenericArguments.Count; i++)
				{
					var genericArgument = genericInstanceType.GenericArguments[i];
					var declaringType = typeDef.ToString();
					yield return new GenericArgument(
						typeDef.GenericParameters[i].Name,
						genericArgument.ToString(),
						declaringType,
						GetGenericDeclaringType(genericArgument as GenericParameter));
				}
			}
		}

		private string? GetGenericDeclaringType(GenericParameter? genericArgument)
		{
			return genericArgument != null
				? genericArgument.DeclaringType?.ToString() ?? genericArgument.DeclaringMethod.DeclaringType.ToString()
				: null;
		}

		private IEnumerable<GenericArgument> GetGenericArguments(FieldReference fieldRef)
		{
			var fieldDef = fieldRef.ToFieldDefinition();
			foreach (var arg in GetGenericArguments(fieldRef.DeclaringType, fieldDef.DeclaringType))
			{
				yield return arg;
			}
		}
	}
}
