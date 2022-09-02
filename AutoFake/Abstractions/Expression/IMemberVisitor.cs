using System.Linq.Expressions;
using System.Reflection;

namespace AutoFake.Abstractions.Expression
{
	internal interface IMemberVisitor<T> : IExecutableMemberVisitor<T>
	{
		T Visit(NewExpression newExpression, ConstructorInfo constructorInfo);
		
	}
}
