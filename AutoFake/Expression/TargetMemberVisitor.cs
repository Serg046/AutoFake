using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace AutoFake.Expression
{
    internal class TargetMemberVisitor : IMemberVisitor
    {
        private readonly IMemberVisitor _requestedVisitor;
        private readonly Type _targeType;

        public TargetMemberVisitor(IMemberVisitor requestedVisitor, Type targetType)
        {
            _requestedVisitor = requestedVisitor;
            _targeType = targetType;
        }

        public void Visit(NewExpression newExpression, ConstructorInfo constructorInfo)
        {
            var paramTypes = constructorInfo.GetParameters().Select(p => p.ParameterType).ToArray();
            var constructor = _targeType.GetConstructor(paramTypes);

            _requestedVisitor.Visit(newExpression, constructor);
        }

        public void Visit(MethodCallExpression methodExpression, MethodInfo methodInfo)
        {
            var paramTypes = methodInfo.GetParameters().Select(p => p.ParameterType).ToArray();
            var method = _targeType.GetMethod(methodInfo.Name, paramTypes);

            _requestedVisitor.Visit(methodExpression, method);
        }

        public void Visit(PropertyInfo propertyInfo)
        {
            var property = _targeType.GetProperty(propertyInfo.Name);
            _requestedVisitor.Visit(property);
        }

        public void Visit(FieldInfo fieldInfo)
        {
            var field = _targeType.GetField(fieldInfo.Name);
            _requestedVisitor.Visit(field);
        }
    }
}
