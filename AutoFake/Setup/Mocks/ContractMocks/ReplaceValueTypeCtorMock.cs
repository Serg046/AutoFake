using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using AutoFake.Abstractions;
using AutoFake.Abstractions.Setup.Mocks;

namespace AutoFake.Setup.Mocks.ContractMocks
{
	internal class ReplaceValueTypeCtorMock : IMockInjector
	{
		private readonly TypeReference _typeReference;
		private readonly ITypeInfo _typeInfo;

		public ReplaceValueTypeCtorMock(TypeReference typeReference, ITypeInfo typeInfo)
		{
			_typeReference = typeReference;
			_typeInfo = typeInfo;
		}

		public bool IsSourceInstruction(MethodDefinition method, Instruction instruction, IEnumerable<GenericArgument> genericArguments)
			=> IsValidOpCode(instruction.OpCode) && instruction.Operand is TypeReference typeRef &&
			   typeRef.GetElementType().FullName == _typeReference.FullName;

		private static bool IsValidOpCode(OpCode opCode)
			=> opCode == OpCodes.Initobj || opCode == OpCodes.Box || opCode == OpCodes.Unbox || opCode == OpCodes.Unbox_Any;

		public void Inject(IEmitter emitter, Instruction instruction)
		{
			var typeRef = (TypeReference)instruction.Operand;
			instruction.Operand = _typeInfo.ImportToSourceAsm(typeRef);
		}

		public override int GetHashCode() => _typeReference.GetHashCode();

		public override bool Equals(object? obj) => obj is ReplaceValueTypeCtorMock mock && mock._typeReference == _typeReference;
	}
}
