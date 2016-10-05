using System;
using System.Linq;
using System.Linq.Expressions;
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

        public TReturn GetStateValue<TReturn>(Expression<Func<T, TReturn>> executeFunc)
        {
            Guard.IsNotNull(executeFunc);
            return GetStateValue<TReturn>(executeFunc.Body);
        }
    }

    public class Fake
    {
        private readonly FakeGenerator _fakeGenerator;
        private string _assemblyFileName;
        private GeneratedObject _currentGeneratedObject;
        private bool _isTestMethodExecuted;

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

        public VerifiableMockInstaller Verify(Expression<Action> voidInstanceSetupFunc)
        {
            Guard.IsNotNull(voidInstanceSetupFunc);
            return new VerifiableMockInstaller(Setups, ExpressionUtils.GetMethodInfo(voidInstanceSetupFunc), GetSetupArguments(voidInstanceSetupFunc.Body), true);
        }

        //---------------------------------------------------------------------------------------------------------

        protected object Execute(LambdaExpression expression)
        {
            if (_isTestMethodExecuted)
                throw new InvalidOperationException("Please call ResetSetups() method to clear state");

            _currentGeneratedObject = _fakeGenerator.Generate(Setups, ExpressionUtils.GetMethodInfo(expression));
            var testMethod = new TestMethod(_currentGeneratedObject);
            var result = testMethod.Execute(expression);

            if (_assemblyFileName != null)
                _fakeGenerator.Save(_assemblyFileName);

            _isTestMethodExecuted = true;

            return result;
        }

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

        //---------------------------------------------------------------------------------------------------------

        protected TReturn GetStateValue<TReturn>(Expression executeFunc)
        {
            if (!_isTestMethodExecuted)
                throw new InvalidOperationException("The test method is not executed yet.");

            var result = ExpressionUtils.ExecuteExpression(_currentGeneratedObject, executeFunc);
            return (TReturn)result;
        }

        public TReturn GetStateValue<TReturn>(Expression<Func<TReturn>> executeFunc)
        {
            Guard.IsNotNull(executeFunc);
            return GetStateValue<TReturn>(executeFunc.Body);
        }

        //---------------------------------------------------------------------------------------------------------

        public void ResetSetups()
        {
            Setups.Clear();
            _isTestMethodExecuted = false;
        }
    }
}
