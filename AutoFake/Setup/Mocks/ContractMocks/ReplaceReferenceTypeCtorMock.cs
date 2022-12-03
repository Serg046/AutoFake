using System.Collections.Generic;
using AutoFake.Abstractions;
using AutoFake.Abstractions.Setup.Mocks.ContractMocks;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup.Mocks.ContractMocks;

internal class ReplaceReferenceTypeCtorMock : IReplaceReferenceTypeCtorMock
{
	private readonly TypeReference _typeReference;
	private readonly ITypeInfo _typeInfo;

	public ReplaceReferenceTypeCtorMock(TypeReference typeReference, ITypeInfo typeInfo)
	{
		_typeReference = typeReference;
		_typeInfo = typeInfo;
	}

	public bool IsSourceInstruction(MethodDefinition method, Instruction instruction, IEnumerable<IGenericArgument> genericArguments)
		=> instruction.OpCode == OpCodes.Newobj && instruction.Operand is MethodReference mRef &&
		   mRef.DeclaringType.GetElementType().FullName == _typeReference.FullName;

	public void Inject(IEmitter emitter, Instruction instruction)
	{
		var method = (MethodReference)instruction.Operand;
		instruction.Operand = _typeInfo.ImportToSourceAsm(method);
	}

	public override int GetHashCode() => (_typeReference.ToString() + nameof(ReplaceReferenceTypeCtorMock)).GetHashCode();

	public override bool Equals(object? obj) => obj is ReplaceReferenceTypeCtorMock mock && mock._typeReference.ToString() == _typeReference.ToString();
}
