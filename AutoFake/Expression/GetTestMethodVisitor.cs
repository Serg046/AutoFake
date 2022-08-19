using System;
using System.Linq.Expressions;
using System.Reflection;
using AutoFake.Abstractions.Expression;
using AutoFake.Exceptions;

namespace AutoFake.Expression
{
	internal class GetTestMethodVisitor : IMemberVisitor
	{
		private MethodBase? _methodInfo;

		public MethodBase Method => _methodInfo
			?? throw new InvalidOperationException($"{nameof(Method)} is not set. Please run {nameof(Visit)}() method.");

		public void Visit(NewExpression newExpression, ConstructorInfo constructorInfo) => _methodInfo = constructorInfo;

		public void Visit(MethodCallExpression methodExpression, MethodInfo methodInfo) => _methodInfo = methodInfo;

		public void Visit(PropertyInfo propertyInfo) => _methodInfo = propertyInfo.GetGetMethod(true);

		public void Visit(FieldInfo fieldInfo)
		{
			throw new NotSupportedExpressionException("Cannot execute a field. The member must have a body.");
		}
	}
}
