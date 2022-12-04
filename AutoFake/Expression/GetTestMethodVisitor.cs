using System;
using System.Linq.Expressions;
using System.Reflection;
using AutoFake.Abstractions.Expression;

namespace AutoFake.Expression;

internal class GetTestMethodVisitor : IGetTestMethodVisitor
{
	public MethodBase Visit(NewExpression newExpression, ConstructorInfo constructorInfo) => constructorInfo;

	public MethodBase Visit(MethodCallExpression methodExpression, MethodInfo methodInfo) => methodInfo;

	public MethodBase Visit(PropertyInfo propertyInfo) => propertyInfo.GetGetMethod(true)
		?? throw new NotSupportedException("The property must contain the getter.");

	public MethodBase Visit(FieldInfo fieldInfo) => throw new NotSupportedException("Cannot execute a field. The member must have a body.");
}
