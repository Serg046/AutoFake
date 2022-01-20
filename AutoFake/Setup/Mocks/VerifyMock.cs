using System.Linq;
using AutoFake.Expression;
using Mono.Cecil.Cil;

namespace AutoFake.Setup.Mocks
{
    internal class VerifyMock : SourceMemberMock
    {
        public VerifyMock(
	        IProcessorFactory processorFactory,
	        IExecutionContext.Create getExecutionContext,
	        IInvocationExpression invocationExpression)
            : base(processorFactory, getExecutionContext, invocationExpression)
        {
        }

        public override void Inject(IEmitter emitter, Instruction instruction)
        {
            var processor = ProcessorFactory.CreateProcessor(emitter, instruction);
			var arguments = processor.RecordMethodCall(SetupBodyField, ExecutionContext,
				SourceMember.GetParameters().Select(p => p.ParameterType).ToReadOnlyList());
            emitter.InsertBefore(instruction, Instruction.Create(OpCodes.Pop));
			processor.PushMethodArguments(arguments);
        }
    }
}
