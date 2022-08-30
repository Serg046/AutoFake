using AutoFake.Abstractions.Setup;

namespace AutoFake.Abstractions.Expression
{
	internal interface IInvocationExpression
	{
		bool ThrowWhenArgumentsAreNotMatched { get; set; }
		T AcceptMemberVisitor<T>(IMemberVisitor<T> visitor);
		ISourceMember GetSourceMember();
	}
}
