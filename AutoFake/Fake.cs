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
        private readonly object[] _contructorArgs;
        private readonly FakeGenerator<T> _fakeGenerator;
        private string _assemblyFileName;

        internal Fake(object[] contructorArgs)
        {
            _contructorArgs = contructorArgs;
            _fakeGenerator = new FakeGenerator<T>(contructorArgs);

            Setups = new List<FakeSetupPack>();
        }

        internal List<FakeSetupPack> Setups { get; }

        public FakeSetup<T, TReturn> Setup<TReturn>(Expression<Func<T, TReturn>> setupFunc)
        {
            return new FakeSetup<T, TReturn>(this, ExpressionUtils.GetMethodInfo(setupFunc));
        }

        public void SaveFakeAssembly(string fileName) => _assemblyFileName = fileName;

        public TReturn Execute<TReturn>(Expression<Func<T, TReturn>> executeFunc)
        {
            var instance = _fakeGenerator.Generate(Setups);
            if (_assemblyFileName != null)
                _fakeGenerator.Save(_assemblyFileName);

            var counter = 0;
            var generatedType = instance.GetType();
            foreach (var setup in Setups)
            {
                var fieldName = _fakeGenerator.GetFieldName(setup, ++counter);
                var field = generatedType.GetField(fieldName);
                field.SetValue(instance, setup.ReturnObject);
            }

            object result;
            if (executeFunc.Body is MethodCallExpression)
            {
                var methodCallExpression = (MethodCallExpression)executeFunc.Body;
                var methodName = methodCallExpression.Method;
                var arguments = methodCallExpression.Arguments.Cast<ConstantExpression>().Select(c => c.Value).ToArray();
                var method = generatedType.GetMethod(methodCallExpression.Method.Name,
                    methodCallExpression.Method.GetParameters().Select(p => p.ParameterType).ToArray());
                result = method.Invoke(instance, arguments);
            }
            else
                throw new InvalidOperationException($"Ivalid expression format. Source: {executeFunc.Body.ToString()}.");

            return (TReturn)result;
        }
    }
}
