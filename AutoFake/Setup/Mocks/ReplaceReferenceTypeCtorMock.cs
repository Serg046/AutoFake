using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using AutoFake.Abstractions;
using AutoFake.Abstractions.Setup.Mocks;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup.Mocks
{
	internal class ReplaceReferenceTypeCtorMock : IMock
	{
		private readonly TypeReference _typeReference;
		private readonly ICecilFactory _cecilFactory;

		public ReplaceReferenceTypeCtorMock(TypeReference typeReference, ICecilFactory cecilFactory)
		{
			_typeReference = typeReference;
			_cecilFactory = cecilFactory;
		}

		public bool IsSourceInstruction(MethodDefinition method, Instruction instruction, IEnumerable<GenericArgument> genericArguments)
			=> instruction.OpCode == OpCodes.Newobj && instruction.Operand is MethodReference mRef &&
			   _typeReference.ToString() == mRef.DeclaringType.ToString();

		[ExcludeFromCodeCoverage]
		public void BeforeInjection(MethodDefinition method)
		{
		}

		public void Inject(IEmitter emitter, Instruction instruction)
		{
			var method = (MethodReference)instruction.Operand;
			instruction.Operand = method.ReplaceDeclaringType(_typeReference, _cecilFactory);
		}

		[ExcludeFromCodeCoverage]
		public void AfterInjection(IEmitter emitter)
		{
		}

		public void Initialize(Type? type)
		{
		}

		public override int GetHashCode() => _typeReference.ToString().GetHashCode();

		public override bool Equals(object? obj) => obj is ReplaceReferenceTypeCtorMock mock && mock._typeReference.ToString() == _typeReference.ToString();
    }
}
