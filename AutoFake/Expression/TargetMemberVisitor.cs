using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using AutoFake.Abstractions.Expression;

namespace AutoFake.Expression;

internal class TargetMemberVisitor<T> : ITargetMemberVisitor<T>
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
		var flags = (methodInfo.IsStatic ? BindingFlags.Static : BindingFlags.Instance) | BindingFlags.Public | BindingFlags.NonPublic;
		var methodName = methodInfo.GetFullMethodName();
		var methodCandidates = _targetType.GetMethods(flags).Where(m => m.Name == methodName);
		var method = GetMethod(methodCandidates, methodInfo, methodName);
		return _requestedVisitor.Visit(methodExpression, method);
	}

	private MethodInfo GetMethod(IEnumerable<MethodInfo> methodCandidates, MethodInfo originalMethod, string targetMethodName)
	{
		if (originalMethod.IsGenericMethod)
		{
			var openMethod = originalMethod.GetGenericMethodDefinition();
			var targetMethod = GetMethodOrThrow(methodCandidates, openMethod, targetMethodName);
			return targetMethod.MakeGenericMethod(originalMethod.GetGenericArguments());
		}

		return GetMethodOrThrow(methodCandidates, originalMethod, targetMethodName);
	}

	private MethodInfo GetMethodOrThrow(IEnumerable<MethodInfo> methodCandidates, MethodInfo originalMethod, string targetMethodName)
	{
		var contract = originalMethod.ToString()!;
		if (originalMethod.Name != targetMethodName)
		{
			contract = Regex.Replace(contract, $"(.+ )({originalMethod.Name})(\\(|\\[.*)",
				match => $"{match.Groups[1].Value}{targetMethodName}{match.Groups[3].Value}");
		}

		return methodCandidates.SingleOrDefault(m => m.ToString() == contract)
			?? throw new MissingMethodException(_targetType.FullName, originalMethod.Name);
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
