using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AutoFake.Abstractions.Expression;

namespace AutoFake.Expression
{
	internal class TargetMemberVisitor<T> : IExecutableMemberVisitor<T>
	{
		private readonly IExecutableMemberVisitor<T> _requestedVisitor;
		private readonly Type _targetType;

		public TargetMemberVisitor(IExecutableMemberVisitor<T> requestedVisitor, Type targetType)
		{
			_requestedVisitor = requestedVisitor;
			_targetType = targetType;
		}

		public T Visit(MethodCallExpression methodExpression, MethodInfo methodInfo)
		{
			var flags = methodInfo.IsStatic ? BindingFlags.Static : BindingFlags.Instance;
			flags |= methodInfo.IsPublic ? BindingFlags.Public : BindingFlags.NonPublic;
			var methodCandidates = _targetType.GetMethods(flags).Where(m => m.Name == methodInfo.Name);

			MethodInfo method;
			if (methodInfo.IsGenericMethod)
			{
				var contract = methodInfo.GetGenericMethodDefinition().ToString()!;
				method = GetMethod(methodCandidates, contract, methodInfo.Name);
				method = method.MakeGenericMethod(methodInfo.GetGenericArguments());
			}
			else
			{
				var contract = methodInfo.ToString()!;
				method = GetMethod(methodCandidates, contract, methodInfo.Name);
			}

			return _requestedVisitor.Visit(methodExpression, method);
		}

		private MethodInfo GetMethod(IEnumerable<MethodInfo> methods, string contract, string methodName)
		{
			return methods.SingleOrDefault(m => m.ToString() == contract) ?? throw new MissingMethodException(_targetType.FullName, methodName);
		}

		public T Visit(PropertyInfo propertyInfo)
		{
			var property = _targetType.GetProperty(propertyInfo.Name) ?? throw new MissingMemberException("Cannot find a property");
			return _requestedVisitor.Visit(property);
		}

		public T Visit(FieldInfo fieldInfo)
		{
			var field = _targetType.GetField(fieldInfo.Name) ?? throw new MissingMemberException("Cannot find a field");
			return _requestedVisitor.Visit(field);
		}
	}
}
