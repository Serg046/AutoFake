using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup.Mocks
{
	internal class ReplaceReferenceTypeCtorMock : IMock
	{
		private readonly TypeReference _typeReference;

		public ReplaceReferenceTypeCtorMock(TypeReference typeReference)
		{
			_typeReference = typeReference;
		}

		public bool IsSourceInstruction(MethodDefinition method, Instruction instruction)
			=> instruction != null && instruction.OpCode == OpCodes.Newobj &&
			   instruction.Operand is MethodReference mRef && _typeReference.ToString() == mRef.DeclaringType.ToString();

		[ExcludeFromCodeCoverage]
		public void BeforeInjection(MethodDefinition method)
		{
		}

		public void Inject(IEmitter emitter, Instruction instruction)
		{
			var method = (MethodReference)instruction.Operand;
			instruction.Operand = method.ReplaceDeclaringType(_typeReference);
		}

		[ExcludeFromCodeCoverage]
		public void AfterInjection(IEmitter emitter)
		{
		}

		public IList<object> Initialize(Type? type) => new List<object>();

		public override int GetHashCode() => _typeReference.ToString().GetHashCode();

		public override bool Equals(object obj) => obj is ReplaceReferenceTypeCtorMock mock && mock._typeReference.ToString() == _typeReference.ToString();
    }
}
