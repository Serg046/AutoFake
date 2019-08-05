using AutoFake.Expression;
using Mono.Cecil.Cil;

namespace AutoFake.Setup
{
    internal class VerifyMock : SourceMemberMock
    {
        public VerifyMock(IInvocationExpression invocationExpression) : base(invocationExpression)
        {
        }

        public override void Inject(IMethodMocker methodMocker, ILProcessor ilProcessor, Instruction instruction)
        {
            if (CheckSourceMemberCalls)
            {
                var arguments = methodMocker.SaveMethodCall(ilProcessor, instruction, CheckArguments);
                methodMocker.PushMethodArguments(ilProcessor, instruction, arguments);
            }
        }
    }
}
