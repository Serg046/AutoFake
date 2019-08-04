using AutoFake.Expression;
using Mono.Cecil.Cil;

namespace AutoFake.Setup
{
    internal class SourceMemberInsertMock : SourceMemberMock
    {
        private readonly InsertMock.Location _location;

        public SourceMemberInsertMock(IInvocationExpression invocationExpression,
            MethodDescriptor action, InsertMock.Location location) : base(invocationExpression)
        {
            _location = location;
            Action = action;
        }

        public MethodDescriptor Action { get; }

        public override void Inject(IMethodMocker methodMocker, ILProcessor ilProcessor, Instruction instruction)
        {
            methodMocker.InjectCallback(ilProcessor, instruction, Action,
                beforeInstruction: _location == InsertMock.Location.Top);
        }
    }
}
