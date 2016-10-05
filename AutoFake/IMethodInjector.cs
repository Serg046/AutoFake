using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake
{
    internal interface IMethodInjector
    {
        bool IsMethodInstruction(Instruction instruction);
        bool IsInstalledMethod(MethodReference method);
        bool IsAsyncMethod(MethodDefinition method, out MethodDefinition asyncMethod);
        void Process(ILProcessor ilProcessor, Instruction instruction);
    }
}