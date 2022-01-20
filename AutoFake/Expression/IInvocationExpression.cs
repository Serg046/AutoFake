using AutoFake.Setup;

namespace AutoFake.Expression
{
    internal interface IInvocationExpression
    {
        bool ThrowWhenArgumentsAreNotMatched { get; set; }
        void AcceptMemberVisitor(IMemberVisitor visitor);
        ISourceMember GetSourceMember();
    }
}