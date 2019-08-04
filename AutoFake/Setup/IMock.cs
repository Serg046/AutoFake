using System;
using System.Collections.Generic;
using Mono.Cecil.Cil;

namespace AutoFake.Setup
{
    internal interface IMock
    {
        string UniqueName { get; }
        bool CheckSourceMemberCalls { get; }
        bool IsSourceInstruction(ITypeInfo typeInfo, MethodBody method, Instruction instruction);
        void BeforeInjection(IMocker mocker);
        void Inject(IMethodMocker methodMocker, ILProcessor ilProcessor, Instruction instruction);
        void AfterInjection(IMocker mocker, ILProcessor ilProcessor);
        IList<object> Initialize(MockedMemberInfo mockedMemberInfo, Type type);
    }
}