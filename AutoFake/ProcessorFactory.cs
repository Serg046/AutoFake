using Mono.Cecil.Cil;

namespace AutoFake
{
    internal class ProcessorFactory : IProcessorFactory
    {
	    public ProcessorFactory(ITypeInfo typeInfo, IAssemblyWriter assemblyWriter)
	    {
		    AssemblyWriter = assemblyWriter;
		    TypeInfo = typeInfo;
	    }

        public ITypeInfo TypeInfo { get; }

        public IAssemblyWriter AssemblyWriter { get; }

        public IPrePostProcessor CreatePrePostProcessor() => new PrePostProcessor(TypeInfo, AssemblyWriter);

        public IProcessor CreateProcessor(IEmitter emitter, Instruction instruction)
            => new Processor(emitter, instruction);
    }
}
