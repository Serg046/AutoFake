using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;

namespace AutoFake.Setup.Mocks
{
	internal class ReplaceValueTypeCtorMock : IMock
	{
		private readonly TypeReference _typeReference;

		public ReplaceValueTypeCtorMock(TypeReference typeReference)
		{
			_typeReference = typeReference;
		}

		public bool IsSourceInstruction(MethodDefinition method, Instruction instruction, IEnumerable<GenericArgument> genericArguments)
			=> IsValidOpCode(instruction.OpCode) && instruction.Operand is TypeReference typeRef &&
			   _typeReference.ToString() == typeRef.ToString();

		private static bool IsValidOpCode(OpCode opCode)
			=> opCode == OpCodes.Initobj || opCode == OpCodes.Box || opCode == OpCodes.Unbox || opCode == OpCodes.Unbox_Any;

		[ExcludeFromCodeCoverage]
		public void BeforeInjection(MethodDefinition method)
		{
		}

		public void Inject(IEmitter emitter, Instruction instruction)
		{
			instruction.Operand = _typeReference;
		}

		[ExcludeFromCodeCoverage]
		public void AfterInjection(IEmitter emitter)
		{
		}

		public void Initialize(Type? type)
		{
		}

		public override int GetHashCode() => _typeReference.GetHashCode();

		public override bool Equals(object? obj) => obj is ReplaceValueTypeCtorMock mock && mock._typeReference == _typeReference;
    }
}
