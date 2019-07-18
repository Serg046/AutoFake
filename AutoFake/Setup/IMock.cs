using System;
using System.Collections.Generic;
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
        IList<object> Initialize(MockedMemberInfo mockedMemberInfo, Type type);
        bool IsInstalledInstruction(ITypeInfo typeInfo, Instruction instruction);
        bool IsAsyncMethod(MethodDefinition method, out MethodDefinition asyncMethod);
    }
}