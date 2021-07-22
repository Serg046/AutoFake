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
            var processor = ProcessorFactory.CreateProcessor(emitter, instruction);
			var arguments = processor.SaveMethodCall(SetupBodyField, ExecutionContext,
				SourceMember.GetParameters().Select(p => p.ParameterType).ToList());
            emitter.InsertBefore(instruction, Instruction.Create(OpCodes.Pop));
			processor.PushMethodArguments(arguments);
        }
    }
}
