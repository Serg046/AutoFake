using AutoFake.Abstractions.Setup;

namespace AutoFake.Abstractions.Expression
{
	internal interface IInvocationExpression
	{
		bool ThrowWhenArgumentsAreNotMatched { get; set; }
		ISourceMember GetSourceMember();
		T AcceptMemberVisitor<T>(IExecutableMemberVisitor<T> visitor);
		T AcceptMemberVisitor<T>(IMemberVisitor<T> visitor);
	}
}
