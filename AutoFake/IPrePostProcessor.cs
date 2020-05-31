using System;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake
{
    internal interface IPrePostProcessor
    {
        FieldDefinition GenerateCallsAccumulator(string name, MethodBody method);
        FieldDefinition GenerateRetValueField(string name, Type returnType);
        FieldDefinition GenerateField(string name, Type returnType);
        FieldDefinition GenerateSetupBodyField(string name);
        void InjectVerification(IEmitter emitter, bool checkArguments, MethodDescriptor expectedCalls,
            FieldDefinition setupBody, FieldDefinition callsAccumulator);
        TypeReference GetTypeReference(Type type);
    }
}