using System.Collections.Generic;
using AutoFake.Setup;

namespace AutoFake.Expression
{
    internal interface IInvocationExpression
    {
        void AcceptMemberVisitor(IMemberVisitor visitor);
        IList<FakeArgument> GetArguments();
        ISourceMember GetSourceMember();
    }
}