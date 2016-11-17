using System;
using System.Linq.Expressions;
using System.Reflection;

namespace AutoFake
{
    internal class GetValueMemberVisitor : IMemberVisitor
    {
        private readonly object _instance;
        private bool _isRuntimeValueSet;
        private object _runtimeValue;

        public GetValueMemberVisitor(GeneratedObject generatedObject)
        {
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

            RuntimeValue = GetValueSafe(() => Expression.Lambda(callExpression).Compile().DynamicInvoke());

            _isRuntimeValueSet = true;
        }

        public void Visit(PropertyInfo propertyInfo)
        {
            RuntimeValue = GetValueSafe(() => propertyInfo.GetValue(_instance, null));
            _isRuntimeValueSet = true;
        }

        private object GetValueSafe(Func<object> func)
        {
            try
            {
                return func();
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw ex.InnerException;
            }
        }

        public void Visit(FieldInfo fieldInfo)
        {
            RuntimeValue = fieldInfo.GetValue(_instance);
            _isRuntimeValueSet = true;
        }
    }
}
