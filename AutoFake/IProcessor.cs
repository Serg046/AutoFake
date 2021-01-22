﻿using System;
using System.Collections.Generic;
using AutoFake.Setup.Mocks;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake
{
    internal interface IProcessor
    {
        void InjectClosure(FieldDefinition closure, InsertMock.Location location);
        void PushMethodArguments(IEnumerable<VariableDefinition> variables);
        void RemoveInstruction(Instruction instruction);
        void RemoveStackArgument();
        void ReplaceToRetValueField(FieldDefinition retField);
        IList<VariableDefinition> SaveMethodCall(FieldDefinition setupBody, FieldDefinition executionContext, IEnumerable<Type> argumentTypes);
    }
}