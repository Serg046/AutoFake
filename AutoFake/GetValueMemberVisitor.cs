using System;
using System.Linq.Expressions;
using System.Reflection;
using GuardExtensions;

namespace AutoFake
{
    internal class GetValueMemberVisitor : IMemberVisitor
    {
        private readonly object _instance;
        private bool _isRuntimeValueSet;
        private object _runtimeValue;

        public GetValueMemberVisitor(GeneratedObject generatedObject)
        {
            Guard.IsNotNull(generatedObject);
            _instance = generatedObject.Instance;
        }

        public object RuntimeValue
        {
            get
            {
                if (!_isRuntimeValueSet)
                    throw new InvalidOperationException($"RuntimeValue is not set. Please run {nameof(Visit)}() method.");
                return _runtimeValue;
            }
            private set { _runtimeValue = value; }
        }

        public void Visit(MethodCallExpression methodExpression, MethodInfo methodInfo)
        {
            var instanceExpr = _instance == null || methodInfo.IsStatic ? null : Expression.Constant(_instance);
            var callExpression = Expression.Call(instanceExpr, methodInfo, methodExpression.Arguments);

            RuntimeValue = Expression.Lambda(callExpression).Compile().DynamicInvoke();
            _isRuntimeValueSet = true;
        }

        public void Visit(PropertyInfo propertyInfo)
        {
            RuntimeValue = propertyInfo.GetValue(_instance, null);
            _isRuntimeValueSet = true;
        }

        public void Visit(FieldInfo fieldInfo)
        {
            RuntimeValue = fieldInfo.GetValue(_instance);
            _isRuntimeValueSet = true;
        }
    }
}
