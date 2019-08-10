using AutoFake.Expression;
using Mono.Cecil.Cil;

namespace AutoFake.Setup.Mocks
{
    internal class VerifyMock : SourceMemberMock
    {
        public VerifyMock(IProcessorFactory processorFactory, IInvocationExpression invocationExpression)
            : base(processorFactory, invocationExpression)
        {
        }

        public override void Inject(IEmitter emitter, Instruction instruction)
        {
            if (CheckSourceMemberCalls)
            {
                var processor = new Processor(ProcessorFactory.TypeInfo, emitter, instruction);
                var arguments = processor.SaveMethodCall(CallsAccumulator, CheckArguments);
                processor.PushMethodArguments(arguments);
            }
        }
    }
}
