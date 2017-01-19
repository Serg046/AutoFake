using System;
using System.Linq.Expressions;
using System.Reflection;
using AutoFake.Exceptions;

namespace AutoFake.Expression
{
    internal class GetTestMethodVisitor : IMemberVisitor
    {
        private MethodBase _methodInfo;
        private bool _isRuntimeValueSet;

        public MethodBase Method
        {
            get
            {
                if (!_isRuntimeValueSet)
                    throw new InvalidOperationException($"{nameof(Method)} is not set. Please run {nameof(Visit)}() method.");
                return _methodInfo;
            }
            private set
            {
                _methodInfo = value;
                _isRuntimeValueSet = true;
            }
        }

        public void Visit(NewExpression newExpression, ConstructorInfo constructorInfo) => Method = constructorInfo;

        public void Visit(MethodCallExpression methodExpression, MethodInfo methodInfo) => Method = methodInfo;

        public void Visit(PropertyInfo propertyInfo) => Method = propertyInfo.GetGetMethod(true);

        public void Visit(FieldInfo fieldInfo)
        {
            throw new NotSupportedExpressionException("Cannot execute a field. The member must have a body.");
        }
    }
}
