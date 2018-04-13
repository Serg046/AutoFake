using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup
{
    internal interface IMock
    {
        void PrepareForInjecting(IMocker mocker);
        void Inject(IMethodMocker methodMocker, ILProcessor ilProcessor, Instruction instruction);
        void Initialize(MockedMemberInfo mockedMemberInfo, GeneratedObject generatedObject);
        void Verify(MockedMemberInfo mockedMemberInfo, GeneratedObject generatedObject);
        bool IsInstalledInstruction(ITypeInfo typeInfo, Instruction instruction);
        bool IsAsyncMethod(MethodDefinition method, out MethodDefinition asyncMethod);
    }
}