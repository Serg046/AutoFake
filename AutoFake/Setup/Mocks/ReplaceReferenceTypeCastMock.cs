using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup.Mocks
{
	internal class ReplaceReferenceTypeCastMock : IMock
	{
		private readonly TypeReference _typeReference;

		public ReplaceReferenceTypeCastMock(TypeReference typeReference)
		{
			_typeReference = typeReference;
		}

		public bool IsSourceInstruction(MethodDefinition method, Instruction instruction, IEnumerable<GenericArgument> genericArguments)
			=> instruction != null && instruction.OpCode == OpCodes.Castclass &&
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

		public override int GetHashCode() => _typeReference.ToString().GetHashCode();

		public override bool Equals(object obj) => obj is ReplaceReferenceTypeCastMock mock && mock._typeReference.ToString() == _typeReference.ToString();
	}
}
