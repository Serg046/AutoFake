using System.Linq.Expressions;
using System.Reflection;

namespace AutoFake.Abstractions.Expression
{
	internal interface IMemberVisitor
	{
		void Visit(NewExpression newExpression, ConstructorInfo constructorInfo);
		void Visit(MethodCallExpression methodExpression, MethodInfo methodInfo);
		void Visit(PropertyInfo propertyInfo);
		void Visit(FieldInfo fieldInfo);
	}
}
