using System;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup
{
    internal interface IMock
    {
        bool CheckArguments { get; }
        Func<byte, bool> ExpectedCalls { get; }
        ISourceMember SourceMember { get; }
        void PrepareForInjecting(IMocker mocker);
        void Inject(IMethodMocker methodMocker, ILProcessor ilProcessor, Instruction instruction);
        void Initialize(MockedMemberInfo mockedMemberInfo, GeneratedObject generatedObject);
        bool IsInstalledInstruction(ITypeInfo typeInfo, Instruction instruction);
        bool IsAsyncMethod(MethodDefinition method, out MethodDefinition asyncMethod);
    }
}