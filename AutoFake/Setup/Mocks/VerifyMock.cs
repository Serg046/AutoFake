using System;
using System.Linq;
using AutoFake.Expression;
using Mono.Cecil.Cil;

namespace AutoFake.Setup.Mocks
{
    internal class VerifyMock : SourceMemberMock
    {
	    private readonly Func<IEmitter, Instruction, IProcessor> _createProcessor;

	    public VerifyMock(
	        IExecutionContext.Create getExecutionContext,
            IInvocationExpression invocationExpression,
	        Func<IEmitter, Instruction, IProcessor> createProcessor,
	        IPrePostProcessor prePostProcessor)
            : base(getExecutionContext, invocationExpression, prePostProcessor)
	    {
		    _createProcessor = createProcessor;
	    }

        public override void Inject(IEmitter emitter, Instruction instruction)
        {
            var processor = _createProcessor(emitter, instruction);
			var arguments = processor.RecordMethodCall(SetupBodyField, ExecutionContext,
				SourceMember.GetParameters().Select(p => p.ParameterType).ToReadOnlyList());
            emitter.InsertBefore(instruction, Instruction.Create(OpCodes.Pop));
			processor.PushMethodArguments(arguments);
        }
    }
}
