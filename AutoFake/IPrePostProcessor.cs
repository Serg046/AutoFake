﻿using System;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake
{
    internal interface IPrePostProcessor
    {
        FieldDefinition GenerateCallsAccumulator(string name, MethodBody method);
        FieldDefinition GenerateField(string name, Type returnType);
        void InjectVerification(IEmitter emitter, bool checkArguments, FieldDefinition expectedCalls,
            FieldDefinition setupBody, FieldDefinition callsAccumulator);
    }
}