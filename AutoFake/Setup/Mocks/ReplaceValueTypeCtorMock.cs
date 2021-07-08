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

		public bool IsSourceInstruction(MethodDefinition method, Instruction instruction)
			=> instruction != null && instruction.OpCode == OpCodes.Initobj &&
			   instruction.Operand is TypeReference typeRef && _typeReference.ToString() == typeRef.ToString();

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

		public IList<object> Initialize(Type? type) => new List<object>();

		public override int GetHashCode() => _typeReference.GetHashCode();

		public override bool Equals(object obj) => obj is ReplaceValueTypeCtorMock mock && mock._typeReference == _typeReference;
    }
}
