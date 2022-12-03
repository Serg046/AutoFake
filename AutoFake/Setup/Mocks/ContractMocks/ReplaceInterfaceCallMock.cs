using System.Collections.Generic;
using AutoFake.Abstractions;
using AutoFake.Abstractions.Setup.Mocks.ContractMocks;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup.Mocks.ContractMocks
{
	internal class ReplaceInterfaceCallMock : IReplaceInterfaceCallMock
	{
		private readonly TypeReference _typeReference;
		private readonly ITypeInfo _typeInfo;

		public ReplaceInterfaceCallMock(TypeReference typeReference, ITypeInfo typeInfo)
		{
			_typeReference = typeReference;
			_typeInfo = typeInfo;
		}

		public bool IsSourceInstruction(MethodDefinition method, Instruction instruction, IEnumerable<IGenericArgument> genericArguments)
			=> instruction.OpCode.OperandType == OperandType.InlineMethod &&
			   instruction.Operand is MethodReference m &&
			   m.DeclaringType.GetElementType().FullName == _typeReference.FullName;


		public void Inject(IEmitter emitter, Instruction instruction)
		{
			var method = (MethodReference)instruction.Operand;
			instruction.Operand = _typeInfo.ImportToSourceAsm(method);
		}

		public override int GetHashCode() => (_typeReference.ToString() + nameof(ReplaceInterfaceCallMock)).GetHashCode();

		public override bool Equals(object? obj) => obj is ReplaceInterfaceCallMock mock && mock._typeReference.ToString() == _typeReference.ToString();
	}
}
