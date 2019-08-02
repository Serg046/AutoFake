using Mono.Cecil.Cil;
using System;

namespace AutoFake
{
    internal interface IMocker : IMethodMocker
    {
        void GenerateSetupBodyField();
        void GenerateRetValueField(Type returnType);
        void GenerateCallbackField();
        void GenerateCallsCounterFuncField();
        void InjectVerification(ILProcessor ilProcessor, bool checkArguments, bool expectedCalls);
    }
}