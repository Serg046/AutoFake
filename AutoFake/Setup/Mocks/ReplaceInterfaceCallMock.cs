﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup.Mocks
{
    internal class ReplaceInterfaceCallMock : IMock
    {
	    private readonly TypeReference _typeReference;

        public ReplaceInterfaceCallMock(TypeReference typeReference)
        {
	        _typeReference = typeReference;
        }

        public bool IsSourceInstruction(MethodDefinition method, Instruction instruction)
	        => instruction != null && instruction.OpCode.OperandType == OperandType.InlineMethod
	                               && instruction.Operand is MethodReference m
	                               && m.DeclaringType.ToString() == _typeReference.ToString();

        [ExcludeFromCodeCoverage]
        public void BeforeInjection(MethodDefinition method)
        {
        }

        public void Inject(IEmitter emitter, Instruction instruction)
        {
	        var method = (MethodReference)instruction.Operand;
			var newInstruction = Instruction.Create(instruction.OpCode, method.ReplaceDeclaringType(_typeReference));
	        emitter.Replace(instruction, newInstruction);
        }

        [ExcludeFromCodeCoverage]
        public void AfterInjection(IEmitter emitter)
        {
        }

        public IList<object> Initialize(Type? type) => new List<object>();

        public override int GetHashCode() => _typeReference.ToString().GetHashCode();

        public override bool Equals(object obj) => obj is ReplaceInterfaceCallMock mock && mock._typeReference.ToString() == _typeReference.ToString();
    }
}
