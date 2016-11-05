using System;
using System.Collections.Generic;
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

        public void SetStateValue<TReturn>(Expression<Func<T, TReturn>> executeFunc, TReturn value)
        {
            Guard.IsNotNull(executeFunc);
            SetStateValue(executeFunc.Body, value);
        }
    }

    public class Fake
    {
        private readonly FakeGenerator _fakeGenerator;
        private string _assemblyFileName;
        private GeneratedObject _currentGeneratedObject;
        private bool _isTestMethodExecuted;
        private readonly Stack<KeyValuePair<Expression, object>> _setStateValuExpressions; 

        public Fake(Type type, params object[] contructorArgs)
        {
            if (contructorArgs == null)
                contructorArgs = new object[] {null};

            var dependencies = contructorArgs.Select(c =>
            {
                var dependecy = c as FakeDependency;
                return dependecy ?? new FakeDependency(c?.GetType(), c);
            }).ToList();

            var typeInfo = new TypeInfo(type, dependencies);
            var mockerFactory = new MockerFactory();
            _fakeGenerator = new FakeGenerator(typeInfo, mockerFactory);
            _setStateValuExpressions = new Stack<KeyValuePair<Expression, object>>();

            Setups = new SetupCollection();
        }

        internal SetupCollection Setups { get; }

        private List<FakeArgument> GetSetupArguments(Expression expression)
        {
            var result = new List<FakeArgument>();

            if (expression is UnaryExpression)
            {
                result = GetSetupArguments(((UnaryExpression)expression).Operand);
            }
            else if (expression is MethodCallExpression)
            {
                FillSetupArguments(expression, result);
            }

            return result;
        }

        private static void FillSetupArguments(Expression expression, List<FakeArgument> result)
        {
            using (var setupContext = new SetupContext())
            {
                foreach (var argument in ExpressionUtils.GetArguments((MethodCallExpression)expression))
                {
                    var argumentChecker = setupContext.IsCheckerSet
                        ? setupContext.PopChecker()
                        : new EqualityArgumentChecker(argument);
                    var fakeArgument = new FakeArgument(argumentChecker);
                    result.Add(fakeArgument);
                }
            }
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
                throw new InvalidOperationException($"Please call {nameof(ClearState)}() method to clear state");

            _currentGeneratedObject = _fakeGenerator.Generate(Setups, ExpressionUtils.GetMethodInfo(expression));
            SetRequestedData(_currentGeneratedObject);

            var testMethod = new TestMethod(_currentGeneratedObject);
            var result = testMethod.Execute(expression);

            if (_assemblyFileName != null)
                _fakeGenerator.Save(_assemblyFileName);

            _isTestMethodExecuted = true;

            return result;
        }

        private void SetRequestedData(GeneratedObject generatedObject)
        {
            while (_setStateValuExpressions.Count > 0)
            {
                var requestedData = _setStateValuExpressions.Pop();
                var visitor = new SetValueMemberVisitor(generatedObject, requestedData.Value);
                _currentGeneratedObject.AcceptMemberVisitor(requestedData.Key, visitor);
            }
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
            ThrowIfTestMethodIsNotExecuted();

            var visitor = new GetValueMemberVisitor(_currentGeneratedObject);
            _currentGeneratedObject.AcceptMemberVisitor(executeFunc, visitor);
            return (TReturn)visitor.RuntimeValue;
        }

        private void ThrowIfTestMethodIsNotExecuted()
        {
            if (!_isTestMethodExecuted)
                throw new InvalidOperationException("The test method is not executed yet.");
        }

        public TReturn GetStateValue<TReturn>(Expression<Func<TReturn>> executeFunc)
        {
            Guard.IsNotNull(executeFunc);
            return GetStateValue<TReturn>(executeFunc.Body);
        }

        //---------------------------------------------------------------------------------------------------------

        protected void SetStateValue<TReturn>(Expression executeFunc, TReturn value)
        {
            if (!_isTestMethodExecuted)
            {
                _setStateValuExpressions.Push(new KeyValuePair<Expression, object>(executeFunc, value));
            }
            else
            {
                var visitor = new SetValueMemberVisitor(_currentGeneratedObject, value);
                _currentGeneratedObject.AcceptMemberVisitor(executeFunc, visitor);
            }
        }

        public void SetStateValue<TReturn>(Expression<Func<TReturn>> executeFunc, TReturn value)
        {
            Guard.IsNotNull(executeFunc);
            SetStateValue(executeFunc.Body, value);
        }

        //---------------------------------------------------------------------------------------------------------

        public void ClearState()
        {
            Setups.Clear();
            _isTestMethodExecuted = false;
        }
    }
}
