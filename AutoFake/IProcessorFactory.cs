using Mono.Cecil.Cil;

namespace AutoFake
{
    internal interface IProcessorFactory
    {
        ITypeInfo TypeInfo { get; }
        IAssemblyWriter AssemblyWriter { get; }
        IPrePostProcessor CreatePrePostProcessor();
        IProcessor CreateProcessor(IEmitter emitter, Instruction instruction);
    }
}