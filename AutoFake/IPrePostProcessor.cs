using System;
using Mono.Cecil;

namespace AutoFake
{
    internal interface IPrePostProcessor
    {
        FieldDefinition GenerateField(string name, Type returnType);
        void InjectVerification(IEmitter emitter, FieldDefinition setupBody, FieldDefinition executionContext);
    }
}