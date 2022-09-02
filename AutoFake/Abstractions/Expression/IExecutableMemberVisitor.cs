using System.Linq.Expressions;
using System.Reflection;

namespace AutoFake.Abstractions.Expression
{
	internal interface IExecutableMemberVisitor<T>
	{
		T Visit(MethodCallExpression methodExpression, MethodInfo methodInfo);
		T Visit(PropertyInfo propertyInfo);
		T Visit(FieldInfo fieldInfo);
	}
}
