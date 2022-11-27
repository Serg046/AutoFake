using System.Collections.Generic;
using AutoFake.Abstractions;
using AutoFake.Abstractions.Setup.Mocks.ContractMocks;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup.Mocks.ContractMocks
{
	internal class ReplaceTypeCastMock : IReplaceTypeCastMock
	{
		private readonly TypeReference _typeReference;
		private readonly ITypeInfo _typeInfo;
		private readonly OpCode _opCode;

		public ReplaceTypeCastMock(TypeReference typeReference, ITypeInfo typeInfo)
		{
			_typeReference = typeReference;
			_typeInfo = typeInfo;
			_opCode = typeReference.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass;
		}

		public bool IsSourceInstruction(MethodDefinition method, Instruction instruction, IEnumerable<GenericArgument> genericArguments)
			=> instruction.OpCode == _opCode && instruction.Operand is TypeReference typeRef &&
			   typeRef.GetElementType().FullName == _typeReference.FullName;

		public void Inject(IEmitter emitter, Instruction instruction)
		{
			var typeRef = (TypeReference)instruction.Operand;
			instruction.Operand = _typeInfo.ImportToSourceAsm(typeRef);
		}

		public override int GetHashCode() => (_typeReference.ToString() + nameof(ReplaceTypeCastMock)).GetHashCode();

		public override bool Equals(object? obj) => obj is ReplaceTypeCastMock mock && mock._typeReference.ToString() == _typeReference.ToString();
	}
}
