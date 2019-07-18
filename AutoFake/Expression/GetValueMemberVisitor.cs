using System;
using System.Linq.Expressions;
using System.Reflection;
using AutoFake.Exceptions;
using LinqExpression = System.Linq.Expressions.Expression;

namespace AutoFake.Expression
{
    internal class GetValueMemberVisitor : IMemberVisitor
    {
        private readonly object _instance;
        private bool _isRuntimeValueSet;
        private object _runtimeValue;

        public GetValueMemberVisitor(FakeObjectInfo generatedObject)
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

        public void Visit(NewExpression newExpression, ConstructorInfo constructorInfo)
        {
            throw new NotSupportedExpressionException("Cannot execute constructor because the instance is already built.");
        }

        public void Visit(MethodCallExpression methodExpression, MethodInfo methodInfo)
        {
            var instanceExpr = _instance == null || methodInfo.IsStatic ? null : LinqExpression.Constant(_instance);
            var callExpression = LinqExpression.Call(instanceExpr, methodInfo, methodExpression.Arguments);
            var lambda = LinqExpression.Lambda(callExpression).Compile();
            RuntimeValue = GetValue(() => lambda.DynamicInvoke());
        }

        public void Visit(PropertyInfo propertyInfo) => RuntimeValue = GetValue(() => propertyInfo.GetValue(_instance, null));

        private object GetValue(Func<object> func)
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
