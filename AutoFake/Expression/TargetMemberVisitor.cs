using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace AutoFake.Expression
{
    internal class TargetMemberVisitor : IMemberVisitor
    {
        private readonly IMemberVisitor _requestedVisitor;
        private readonly Type _targetType;

        public TargetMemberVisitor(IMemberVisitor requestedVisitor, Type targetType)
        {
            _requestedVisitor = requestedVisitor;
            _targetType = targetType;
        }

        public void Visit(NewExpression newExpression, ConstructorInfo constructorInfo)
        {
            var paramTypes = constructorInfo.GetParameters().Select(p => p.ParameterType).ToArray();
            var constructor = _targetType.GetConstructor(paramTypes)
	            ?? throw new InvalidOperationException("Cannot find a constructor");
            _requestedVisitor.Visit(newExpression, constructor);
        }

        public void Visit(MethodCallExpression methodExpression, MethodInfo methodInfo)
        {
	        var flags = methodInfo.IsStatic ? BindingFlags.Static : BindingFlags.Instance;
	        flags |= methodInfo.IsPublic ? BindingFlags.Public : BindingFlags.NonPublic;
            var methodCandidates = _targetType.GetMethods(flags).Where(m => m.Name == methodInfo.Name);

            MethodInfo method;
            if (methodInfo.IsGenericMethod)
            {
	            var contract = methodInfo.GetGenericMethodDefinition().ToString()!;
	            method = GetMethod(methodCandidates, contract);
	            method = method.MakeGenericMethod(methodInfo.GetGenericArguments());
            }
            else
            {
	            var contract = methodInfo.ToString()!;
	            method = GetMethod(methodCandidates, contract);
            }

            _requestedVisitor.Visit(methodExpression, method);
        }

        private MethodInfo GetMethod(IEnumerable<MethodInfo> methodCandidates, string contract)
        {
	        var methods = methodCandidates.Where(m => m.ToString() == contract).ToList();
	        return methods.Count == 1
                ? methods[0]
				: methods.Single(m => m.DeclaringType == _targetType);
        }

        public void Visit(PropertyInfo propertyInfo)
        {
            var property = _targetType.GetProperty(propertyInfo.Name)
	            ?? throw new InvalidOperationException("Cannot find a property");
            _requestedVisitor.Visit(property);
        }

        public void Visit(FieldInfo fieldInfo)
        {
            var field = _targetType.GetField(fieldInfo.Name) ?? throw new InvalidOperationException("Cannot find a field");
            _requestedVisitor.Visit(field);
        }
    }
}
