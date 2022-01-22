using System;
using Mono.Cecil.Cil;

namespace AutoFake
{
    internal class ProcessorFactory : IProcessorFactory
    {
	    private readonly IPrePostProcessor _prePostProcessor;
	    private readonly Func<IEmitter, Instruction, IProcessor> _createProcessor;

	    public ProcessorFactory(ITypeInfo typeInfo, IAssemblyWriter assemblyWriter,
		    IPrePostProcessor prePostProcessor, Func<IEmitter, Instruction, IProcessor> createProcessor)
	    {
		    _prePostProcessor = prePostProcessor;
		    _createProcessor = createProcessor;
		    AssemblyWriter = assemblyWriter;
		    TypeInfo = typeInfo;
	    }

        public ITypeInfo TypeInfo { get; }

        public IAssemblyWriter AssemblyWriter { get; }

        public IPrePostProcessor CreatePrePostProcessor() => _prePostProcessor;

        public IProcessor CreateProcessor(IEmitter emitter, Instruction instruction) => _createProcessor(emitter, instruction);
    }
}
