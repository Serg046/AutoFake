using AutoFake.Abstractions.Setup;
using LinqExpression = System.Linq.Expressions.Expression;

namespace AutoFake.Abstractions.Expression;

public interface IInvocationExpression
{
	public delegate IInvocationExpression Create(LinqExpression expression);

	bool ThrowWhenArgumentsAreNotMatched { get; set; }
	ISourceMember GetSourceMember();
	T AcceptMemberVisitor<T>(IExecutableMemberVisitor<T> visitor);
	T AcceptMemberVisitor<T>(IMemberVisitor<T> visitor);
}
