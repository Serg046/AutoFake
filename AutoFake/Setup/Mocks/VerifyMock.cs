using System.Linq;
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
                var processor = ProcessorFactory.CreateProcessor(emitter, instruction);
				var arguments = processor.SaveMethodCall(CallsAccumulator, CheckArguments,
					SourceMember.GetParameters().Select(p => p.ParameterType));
				processor.PushMethodArguments(arguments);
			}
        }
    }
}
