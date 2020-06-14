using System;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake
{
    internal interface IPrePostProcessor
    {
        FieldDefinition GenerateCallsAccumulator(string name, MethodBody method);
        FieldDefinition GenerateField(string name, Type returnType);
        void InjectVerification(IEmitter emitter, bool checkArguments, ClosureDescriptor expectedCalls,
            FieldDefinition setupBody, FieldDefinition callsAccumulator);
        TypeReference GetTypeReference(Type type);
    }
}