using System;
using System.Linq.Expressions;
using System.Reflection;

namespace AutoFake.Expression
{
    internal class GetTestMethodVisitor : IMemberVisitor
    {
        private MethodInfo _methodInfo;
        private bool _isRuntimeValueSet;

        public MethodInfo Method
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

        public void Visit(MethodCallExpression methodExpression, MethodInfo methodInfo) => Method = methodInfo;

        public void Visit(PropertyInfo propertyInfo) => Method = propertyInfo.GetGetMethod(true);

        public void Visit(FieldInfo fieldInfo)
        {
            throw new InvalidOperationException("Cannot execute a field. The member must have a body.");
        }
    }
}
