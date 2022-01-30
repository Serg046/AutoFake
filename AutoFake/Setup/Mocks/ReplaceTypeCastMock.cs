using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using AutoFake.Abstractions;
using AutoFake.Abstractions.Setup.Mocks;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup.Mocks
{
	internal class ReplaceTypeCastMock : IMock
	{
		private readonly TypeReference _typeReference;
		private readonly OpCode _opCode;

		public ReplaceTypeCastMock(TypeReference typeReference)
		{
			_typeReference = typeReference;
			_opCode = typeReference.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass;
		}

		public bool IsSourceInstruction(MethodDefinition method, Instruction instruction, IEnumerable<GenericArgument> genericArguments)
			=> instruction.OpCode == _opCode && instruction.Operand is TypeReference typeRef &&
			   _typeReference.ToString() == typeRef.ToString();

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

		public override int GetHashCode() => _typeReference.ToString().GetHashCode();

		public override bool Equals(object? obj) => obj is ReplaceTypeCastMock mock && mock._typeReference.ToString() == _typeReference.ToString();
	}
}
