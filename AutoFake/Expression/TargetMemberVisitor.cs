using System;
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
            var constructor = _targetType.GetConstructor(paramTypes);

            _requestedVisitor.Visit(newExpression, constructor);
        }

        public void Visit(MethodCallExpression methodExpression, MethodInfo methodInfo)
        {
            var paramTypes = methodInfo.GetParameters().Select(p => p.ParameterType).ToArray();
            var method = _targetType.GetMethod(methodInfo.Name, paramTypes);

            _requestedVisitor.Visit(methodExpression, method);
        }

        public void Visit(PropertyInfo propertyInfo)
        {
            var property = _targetType.GetProperty(propertyInfo.Name);
            _requestedVisitor.Visit(property);
        }

        public void Visit(FieldInfo fieldInfo)
        {
            var field = _targetType.GetField(fieldInfo.Name);
            _requestedVisitor.Visit(field);
        }
    }
}
