using AutoFake.Setup;

namespace AutoFake.Expression
{
    internal interface IInvocationExpression
    {
        void AcceptMemberVisitor(IMemberVisitor visitor);
        ISourceMember GetSourceMember();
    }
}