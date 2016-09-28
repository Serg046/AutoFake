using System;
using System.Linq;
using System.Linq.Expressions;
using AutoFake.Exceptions;
using AutoFake.Setup;
using GuardExtensions;

namespace AutoFake
{
    public class Fake<T> : Fake
    {
        public Fake(params object[] contructorArgs) : base(typeof(T), contructorArgs)
        {
        }

        public TReturn Execute<TReturn>(Expression<Func<T, TReturn>> executeFunc)
        {
            Guard.IsNotNull(executeFunc);
            return (TReturn)Execute((LambdaExpression)executeFunc);
        }

        public void Execute(Expression<Action<T>> executeFunc)
        {
            Guard.IsNotNull(executeFunc);
            Execute((LambdaExpression)executeFunc);
        }

        public TReturn CheckState<TReturn>(Expression<Func<T, TReturn>> executeFunc)
        {
            Guard.IsNotNull(executeFunc);
            var result = ExpressionUtils.ExecuteExpression(_currentGeneratedObject, executeFunc.Body);
            return (TReturn)result;
        }

        public void CheckState(Expression<Action<T>> executeFunc)
        {
            Guard.IsNotNull(executeFunc);
            Execute((LambdaExpression)executeFunc);
        }
    }

    public class Fake
    {
        private readonly FakeGenerator _fakeGenerator;
        private string _assemblyFileName;
        internal GeneratedObject _currentGeneratedObject;

        public Fake(Type type, params object[] contructorArgs)
        {
            Guard.IsNotNull(contructorArgs);

            var typeInfo = new TypeInfo(type, contructorArgs);
            var mockerFactory = new MockerFactory();
            _fakeGenerator = new FakeGenerator(typeInfo, mockerFactory);

            Setups = new SetupCollection();
        }

        internal SetupCollection Setups { get; }

        private object[] GetSetupArguments(Expression expression)
        {
            var result = new object[0];

            if (expression is UnaryExpression)
            {
                result = GetSetupArguments(((UnaryExpression)expression).Operand);
            }
            else if (expression is MethodCallExpression)
            {
                result = ExpressionUtils.GetArguments((MethodCallExpression)expression).ToArray();
            }

            return result;
        }

        protected object Execute(LambdaExpression expression)
        {
            if (Setups.Count == 0)
                throw new FakeGeneretingException("Setup pack is not found");

            _currentGeneratedObject = _fakeGenerator.Generate(Setups, ExpressionUtils.GetMethodInfo(expression));
            var testMethod = new TestMethod(_currentGeneratedObject);
            var result = testMethod.Execute(expression);

            if (_assemblyFileName != null)
                _fakeGenerator.Save(_assemblyFileName);

            return result;
        }

        //---------------------------------------------------------------------------------------------------------

        public void SaveFakeAssembly(string fileName)
        {
            Guard.IsNotNull(fileName);
            _assemblyFileName = fileName;
        }

        //---------------------------------------------------------------------------------------------------------

        public ReplaceableMockInstaller<TReturn> Replace<TReturn>(Expression<Func<TReturn>> setupFunc)
        {
            Guard.IsNotNull(setupFunc);
            return new ReplaceableMockInstaller<TReturn>(Setups, ExpressionUtils.GetMethodInfo(setupFunc), GetSetupArguments(setupFunc.Body));
        }

        public ReplaceableMockInstaller<TReturn> Replace<TInput, TReturn>(Expression<Func<TInput, TReturn>> instanceSetupFunc)
        {
            Guard.IsNotNull(instanceSetupFunc);
            return new ReplaceableMockInstaller<TReturn>(Setups, ExpressionUtils.GetMethodInfo(instanceSetupFunc), GetSetupArguments(instanceSetupFunc.Body));
        }

        public ReplaceableMockInstaller Replace<TInput>(Expression<Action<TInput>> voidInstanceSetupFunc)
        {
            Guard.IsNotNull(voidInstanceSetupFunc);
            return new ReplaceableMockInstaller(Setups, ExpressionUtils.GetMethodInfo(voidInstanceSetupFunc), GetSetupArguments(voidInstanceSetupFunc.Body));
        }

        public ReplaceableMockInstaller Replace(Expression<Action> voidInstanceSetupFunc)
        {
            Guard.IsNotNull(voidInstanceSetupFunc);
            return new ReplaceableMockInstaller(Setups, ExpressionUtils.GetMethodInfo(voidInstanceSetupFunc), GetSetupArguments(voidInstanceSetupFunc.Body));
        }

        //---------------------------------------------------------------------------------------------------------

        public VerifiableMockInstaller Verify<TReturn>(Expression<Func<TReturn>> setupFunc)
        {
            Guard.IsNotNull(setupFunc);
            return new VerifiableMockInstaller(Setups, ExpressionUtils.GetMethodInfo(setupFunc), GetSetupArguments(setupFunc.Body), false);
        }

        public VerifiableMockInstaller Verify<TInput, TReturn>(Expression<Func<TInput, TReturn>> instanceSetupFunc)
        {
            Guard.IsNotNull(instanceSetupFunc);
            return new VerifiableMockInstaller(Setups, ExpressionUtils.GetMethodInfo(instanceSetupFunc), GetSetupArguments(instanceSetupFunc.Body), false);
        }

        public VerifiableMockInstaller Verify<TInput>(Expression<Action<TInput>> voidInstanceSetupFunc)
        {
            Guard.IsNotNull(voidInstanceSetupFunc);
            return new VerifiableMockInstaller(Setups, ExpressionUtils.GetMethodInfo(voidInstanceSetupFunc), GetSetupArguments(voidInstanceSetupFunc.Body), true);
        }

        //---------------------------------------------------------------------------------------------------------

        public TReturn Execute<TReturn>(Expression<Func<TReturn>> executeFunc)
        {
            Guard.IsNotNull(executeFunc);
            return (TReturn)Execute((LambdaExpression)executeFunc);
        }

        public void Execute(Expression<Action> executeFunc)
        {
            Guard.IsNotNull(executeFunc);
            Execute((LambdaExpression)executeFunc);
        }
    }
}
