using System.Linq.Expressions;
using System.Reflection;

namespace AutoFake.Abstractions.Expression;

public interface IExecutableMemberVisitor<T>
{
	T Visit(MethodCallExpression methodExpression, MethodInfo methodInfo);
	T Visit(PropertyInfo propertyInfo);
	T Visit(FieldInfo fieldInfo);
}
