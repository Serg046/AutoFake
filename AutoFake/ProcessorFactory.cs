using Mono.Cecil.Cil;

namespace AutoFake
{
    internal class ProcessorFactory : IProcessorFactory
    {
        public ProcessorFactory(ITypeInfo typeInfo) => TypeInfo = typeInfo;

        public ITypeInfo TypeInfo { get; }

        public IPrePostProcessor CreatePrePostProcessor() => new PrePostProcessor(TypeInfo);

        public IProcessor CreateProcessor(IEmitter emitter, Instruction instruction)
            => new Processor(TypeInfo, emitter, instruction);
    }
}
