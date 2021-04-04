using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup.Mocks
{
	internal class ReplaceContractMock : IMock
	{
		private readonly Type _type;
		private readonly TypeReference _typeRef;
		private readonly string _typeContract;

		public ReplaceContractMock(ITypeInfo typeInfo, Type type)
		{
			_type = type;
			_typeRef = typeInfo.ImportReference(type);
			_typeContract = _typeRef.ToString();
		}

		public bool IsSourceInstruction(MethodDefinition method, Instruction instruction)
			=> instruction != null && instruction.OpCode.OperandType == OperandType.InlineMethod
								   && instruction.Operand is MethodReference m && method.ToString() != m.ToString();

		[ExcludeFromCodeCoverage]
		public void BeforeInjection(MethodDefinition method)
		{
		}

		public void Inject(IEmitter emitter, Instruction instruction)
		{
			var method = (MethodReference)instruction.Operand;
			if (method.ReturnType.ToString() == _typeContract)
			{
				method.ReturnType = _typeRef;
			}

			for (var i = 0; i < method.Parameters.Count; i++)
			{
				if (method.Parameters[i].ParameterType.ToString() == _typeContract)
				{
					method.Parameters[i].ParameterType = _typeRef;
				}
			}
		}
		
		[ExcludeFromCodeCoverage]
		public void AfterInjection(IEmitter emitter)
		{
		}

		public IList<object> Initialize(Type? type) => new List<object>();

		public override int GetHashCode() => _type.GetHashCode();

		public override bool Equals(object obj) => obj is ReplaceContractMock mock && mock._type == _type;
	}
}
