using System;
using System.Linq.Expressions;
using System.Reflection;
using AutoFake.Abstractions.Expression;
using AutoFake.Exceptions;
using LinqExpression = System.Linq.Expressions.Expression;

namespace AutoFake.Expression
{
	internal class GetValueMemberVisitor : IMemberVisitor
	{
		private readonly object? _instance;
		private object? _runtimeValue;
		private Type? _type;
		private bool _isRuntimeValueSet;

		public GetValueMemberVisitor(object? instance)
		{
			_instance = instance;
		}

		public object? RuntimeValue
		{
			get => _isRuntimeValueSet ? _runtimeValue : throw new InvalidOperationException($"{nameof(RuntimeValue)} is not set. Please run {nameof(Visit)}() method.");
			private set
			{
				_runtimeValue = value;
				_isRuntimeValueSet = true;
			}
		}

		public Type Type => _type ?? throw new InvalidOperationException($"Type is not set. Please run {nameof(Visit)}() method.");

		public void Visit(NewExpression newExpression, ConstructorInfo constructorInfo)
		{
			throw new NotSupportedExpressionException("Cannot execute constructor because the instance has been already built.");
		}

		public void Visit(MethodCallExpression methodExpression, MethodInfo methodInfo)
		{
			var instanceExpr = _instance == null || methodInfo.IsStatic ? null : LinqExpression.Constant(_instance);
			var callExpression = LinqExpression.Call(instanceExpr, methodInfo, methodExpression.Arguments);
			var lambda = LinqExpression.Lambda(callExpression).Compile();
			_type = methodInfo.ReturnType;
			RuntimeValue = lambda.DynamicInvoke();
		}

		public void Visit(PropertyInfo propertyInfo)
		{
			_type = propertyInfo.PropertyType;
			RuntimeValue = propertyInfo.GetValue(_instance, null);
		}

		public void Visit(FieldInfo fieldInfo)
		{
			_type = fieldInfo.FieldType;
			RuntimeValue = fieldInfo.GetValue(_instance);
		}
	}
}
