using System;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake
{
    internal interface IPrePostProcessor
    {
        VariableDefinition GenerateCallsAccumulator(MethodBody method);
        FieldDefinition GenerateRetValueField(string name, Type returnType);
        FieldDefinition GenerateSetupBodyField(string name);
        void InjectVerification(IEmitter emitter, bool checkArguments, MethodDescriptor expectedCalls,
            FieldDefinition setupBody, VariableDefinition callsAccumulator);
        TypeReference GetTypeReference(Type type);
    }
}