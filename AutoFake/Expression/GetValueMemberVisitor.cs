using System;
using System.Linq.Expressions;
using System.Reflection;

namespace AutoFake.Expression
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
                    throw new InvalidOperationException($"{nameof(RuntimeValue)} is not set. Please run {nameof(Visit)}() method.");
                return _runtimeValue;
            }
            private set
            {
                _runtimeValue = value;
                _isRuntimeValueSet = true;
            }
        }

        public void Visit(MethodCallExpression methodExpression, MethodInfo methodInfo)
        {
            var instanceExpr = _instance == null || methodInfo.IsStatic ? null : System.Linq.Expressions.Expression.Constant(_instance);
            var callExpression = System.Linq.Expressions.Expression.Call(instanceExpr, methodInfo, methodExpression.Arguments);

            RuntimeValue = GetValueSafe(() => System.Linq.Expressions.Expression.Lambda(callExpression).Compile().DynamicInvoke());
        }

        public void Visit(PropertyInfo propertyInfo) => RuntimeValue = GetValueSafe(() => propertyInfo.GetValue(_instance, null));

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

        public void Visit(FieldInfo fieldInfo) => RuntimeValue = fieldInfo.GetValue(_instance);
    }
}
