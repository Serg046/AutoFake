using System;
using System.Linq.Expressions;
using System.Reflection;
using AutoFake.Abstractions.Expression;
using LinqExpression = System.Linq.Expressions.Expression;

namespace AutoFake.Expression;

internal class GetValueMemberVisitor : IGetValueMemberVisitor
{
	private readonly object? _instance;

	public GetValueMemberVisitor(object? instance)
	{
		_instance = instance;
	}

	public (Type, object?) Visit(MethodCallExpression methodExpression, MethodInfo methodInfo)
	{
		var instanceExpr = _instance == null || methodInfo.IsStatic ? null : LinqExpression.Constant(_instance);
		var callExpression = LinqExpression.Call(instanceExpr, methodInfo, methodExpression.Arguments);
		var lambda = LinqExpression.Lambda(callExpression).Compile();
		var type = methodInfo.ReturnType;
		return (type, lambda.DynamicInvoke());
	}

	public (Type, object?) Visit(PropertyInfo propertyInfo)
	{
		var type = propertyInfo.PropertyType;
		return (type, propertyInfo.GetValue(_instance, null));
	}

	public (Type, object?) Visit(FieldInfo fieldInfo)
	{
		var type = fieldInfo.FieldType;
		return (type, fieldInfo.GetValue(_instance));
	}
}
