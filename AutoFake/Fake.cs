using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace AutoFake
{
    public class Fake
    {
        public static Fake<T> For<T>(params object[] contructorArgs) => new Fake<T>(contructorArgs);
    }

    public class Fake<T>
    {
        private readonly FakeGenerator<T> _fakeGenerator;
        private string _assemblyFileName;

        internal Fake(object[] contructorArgs)
        {
            _fakeGenerator = new FakeGenerator<T>(contructorArgs);

            Setups = new List<FakeSetupPack>();
        }

        internal List<FakeSetupPack> Setups { get; }

        public FakeSetup<T, TReturn> Setup<TReturn>(Expression<Func<TReturn>> setupFunc)
        {
            return new FakeSetup<T, TReturn>(this, ExpressionUtils.GetMethodInfo(setupFunc), GetSetupArguments(setupFunc.Body));
        }

        public FakeSetup<T, TReturn> Setup<TInput, TReturn>(Expression<Func<TInput, TReturn>> setupFunc)
        {
            return new FakeSetup<T, TReturn>(this, ExpressionUtils.GetMethodInfo(setupFunc), GetSetupArguments(setupFunc.Body));
        }

        public FakeSetup<T> Setup<TInput>(Expression<Action<TInput>> setupFunc)
        {
            return new FakeSetup<T>(this, ExpressionUtils.GetMethodInfo(setupFunc), GetSetupArguments(setupFunc.Body));
        }

        private object[] GetSetupArguments(Expression expression)
        {
            var result = new object[0];

            if (expression is UnaryExpression)
            {
                result = GetSetupArguments(((UnaryExpression)expression).Operand);
            }
            else if (expression is MethodCallExpression)
            {
                result = ((MethodCallExpression)expression).Arguments
                    .Select(ExpressionUtils.GetArgument).ToArray();
            }

            return result;
        }

        public void SaveFakeAssembly(string fileName) => _assemblyFileName = fileName;

        public TReturn Execute<TReturn>(Expression<Func<T, TReturn>> executeFunc)
        {
            return (TReturn)Execute((LambdaExpression)executeFunc);
        }

        public void Execute(Expression<Action<T>> executeFunc)
        {
            Execute((LambdaExpression)executeFunc);
        }

        private object Execute(LambdaExpression expression)
        {
            if (Setups.Count == 0)
                throw new InvalidOperationException("Setup pack is not found");

            var instance = _fakeGenerator.Generate(Setups, ExpressionUtils.GetMethodInfo(expression));
            if (_assemblyFileName != null)
                _fakeGenerator.Save(_assemblyFileName);

            var generatedType = instance.GetType();
            SetReturnObjects(generatedType);

            var result = GetInvocationResult(expression.Body, instance, generatedType);
            VerifySetups(generatedType, instance);
            return result;
        }

        private void SetReturnObjects(Type generatedType)
        {
            var counter = 0;
            foreach (var setup in Setups.Where(s => !s.IsVoid))
            {
                var fieldName = _fakeGenerator.GetFieldName(setup, counter++);
                var field = generatedType.GetField(fieldName, BindingFlags.Public | BindingFlags.Static);
                field.SetValue(null, setup.ReturnObject);
            }
        }

        private void VerifySetups(Type generatedType, object instance)
        {
            foreach (var setup in Setups.Where(s => s.IsVerifiable))
            {
                var counter = 0;
                foreach (var setupArg in setup.SetupArguments)
                {
                    for (var i = 0; i < setup.ActualCallsCount; i++)
                    {
                        var fldName = _fakeGenerator.GetArgumentFieldName(setup, i * setup.SetupArguments.Length + counter);
                        var fldInfo = generatedType.GetField(fldName, BindingFlags.Public | BindingFlags.Static);
                        var realArg = fldInfo.GetValue(instance);
                        if (!setupArg.Equals(realArg))
                            throw new InvalidOperationException(
                                $"Setup and real arguments are different. Expected: {setupArg}. Actual: {realArg}.");
                    }
                    counter++;
                }
            }
        }

        private object GetInvocationResult(Expression executeFunc, object instance, Type generatedType)
        {
            object result;
            if (executeFunc is MethodCallExpression)
            {
                var methodCallExpression = (MethodCallExpression)executeFunc;
                var arguments = methodCallExpression.Arguments.Select(ExpressionUtils.GetArgument).ToArray();
                var method = generatedType.GetMethod(methodCallExpression.Method.Name,
                    methodCallExpression.Method.GetParameters().Select(p => p.ParameterType).ToArray());
                result = method.Invoke(instance, arguments);
            }
            else if (executeFunc is MemberExpression)
            {
                var propInfo = ((MemberExpression)executeFunc).Member as PropertyInfo;
                var property = generatedType.GetProperty(propInfo.Name);
                result = property.GetValue(instance, null);
            }
            else if (executeFunc is UnaryExpression)
            {
                result = GetInvocationResult(((UnaryExpression)executeFunc).Operand, instance, generatedType);
            }
            else
                throw new InvalidOperationException($"Ivalid expression format. Source: {executeFunc.ToString()}.");
            return result;
        }
    }
}
