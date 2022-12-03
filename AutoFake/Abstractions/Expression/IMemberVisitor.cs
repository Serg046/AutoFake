using System.Linq.Expressions;
using System.Reflection;

namespace AutoFake.Abstractions.Expression;

public interface IMemberVisitor<T> : IExecutableMemberVisitor<T>
{
	T Visit(NewExpression newExpression, ConstructorInfo constructorInfo);
}
