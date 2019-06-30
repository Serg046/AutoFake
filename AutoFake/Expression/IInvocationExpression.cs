using System;
using System.Collections.Generic;
using AutoFake.Setup;

namespace AutoFake.Expression
{
    internal interface IInvocationExpression
    {
        void AcceptMemberVisitor(IMemberVisitor visitor);
        ISourceMember GetSourceMember();
        void MatchArguments(ICollection<object[]> arguments, bool checkArguments, Func<byte, bool> expectedCalls);
    }
}