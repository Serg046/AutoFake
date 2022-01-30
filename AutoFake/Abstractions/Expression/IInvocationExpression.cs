using AutoFake.Abstractions.Setup;

namespace AutoFake.Abstractions.Expression
{
    internal interface IInvocationExpression
    {
        bool ThrowWhenArgumentsAreNotMatched { get; set; }
        void AcceptMemberVisitor(IMemberVisitor visitor);
        ISourceMember GetSourceMember();
    }
}