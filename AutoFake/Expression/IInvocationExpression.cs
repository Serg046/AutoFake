using System.Collections.Generic;
using AutoFake.Setup;

namespace AutoFake.Expression
{
    internal interface IInvocationExpression
    {
        bool ThrowWhenArgumentsAreNotMatched { get; set; }
        void AcceptMemberVisitor(IMemberVisitor visitor);
        ISourceMember GetSourceMember();
        IList<IFakeArgument> GetArguments();
    }
}