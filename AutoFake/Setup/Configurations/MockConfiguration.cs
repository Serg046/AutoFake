using AutoFake.Setup.Mocks;
using System;
using System.Linq.Expressions;
using AutoFake.Abstractions.Setup;
using AutoFake.Abstractions.Setup.Configurations;
using InvocationExpression = AutoFake.Expression.InvocationExpression;

namespace AutoFake.Setup.Configurations
{
	internal class FuncMockConfiguration<TSut, TReturn> : MockConfiguration<TSut>, IFuncMockConfiguration<TSut, TReturn>
	{
        private readonly ExpressionExecutor<TReturn> _executor;

        internal FuncMockConfiguration(InvocationExpression.Create exprFactory, IMockConfigurationFactory cfgFactory,
	        IMockFactory mockFactory, IMockCollection mocks, ExpressionExecutor<TReturn> executor)
	        : base(exprFactory, cfgFactory, mockFactory, mocks)
        {
            _executor = executor;
        }

        public TReturn Execute() => _executor.Execute();
    }

	internal class ActionMockConfiguration<TSut> : MockConfiguration<TSut>, IActionMockConfiguration<TSut>
	{
        private readonly ExpressionExecutor _executor;

        internal ActionMockConfiguration(InvocationExpression.Create exprFactory, IMockConfigurationFactory cfgFactory,
	        IMockFactory mockFactory, IMockCollection mocks, ExpressionExecutor executor)
	        : base(exprFactory, cfgFactory, mockFactory, mocks)
        {
            _executor = executor;
        }

        public void Execute() => _executor.Execute();
    }

    internal abstract class MockConfiguration<TSut>
    {
	    private readonly InvocationExpression.Create _exprFactory;

        internal MockConfiguration(InvocationExpression.Create exprFactory, IMockConfigurationFactory cfgFactory,
	        IMockFactory mockFactory, IMockCollection mocks)
        {
            _exprFactory = exprFactory;
            ConfigurationFactory = cfgFactory;
            MockFactory = mockFactory;
            Mocks = mocks;
        }

        internal IMockConfigurationFactory ConfigurationFactory { get; }

        internal IMockFactory MockFactory { get; }

        internal IMockCollection Mocks { get; }

        public IReplaceMockConfiguration<TSut, TReturn> Replace<TReturn>(Expression<Func<TSut, TReturn>> instanceSetupFunc) => ReplaceImpl<TReturn>(instanceSetupFunc);

        public IReplaceMockConfiguration<TSut, TReturn> Replace<TInput, TReturn>(Expression<Func<TInput, TReturn>> instanceSetupFunc) => ReplaceImpl<TReturn>(instanceSetupFunc);

        public IReplaceMockConfiguration<TSut, TReturn> Replace<TReturn>(Expression<Func<TReturn>> staticSetupFunc) => ReplaceImpl<TReturn>(staticSetupFunc);

        public IRemoveMockConfiguration<TSut> Remove(Expression<Action<TSut>> voidInstanceSetupFunc) => RemoveImpl(voidInstanceSetupFunc);

        public IRemoveMockConfiguration<TSut> Remove<TInput>(Expression<Action<TInput>> voidInstanceSetupFunc) => RemoveImpl(voidInstanceSetupFunc);

        public IRemoveMockConfiguration<TSut> Remove(Expression<Action> voidStaticSetupFunc) => RemoveImpl(voidStaticSetupFunc);

        public IVerifyMockConfiguration Verify<TReturn>(Expression<Func<TSut, TReturn>> instanceSetupFunc)
            => VerifyImpl(instanceSetupFunc);

        public IVerifyMockConfiguration Verify(Expression<Action<TSut>> voidInstanceSetupFunc)
            => VerifyImpl(voidInstanceSetupFunc);

        public IPrependMockConfiguration<TSut> Prepend(Action action)
        {
	        var position = Mocks.Count;
	        Mocks.Add(MockFactory.GetInsertMock(action, InsertMock.Location.Before));
            return ConfigurationFactory.GetInsertMockConfiguration<IPrependMockConfiguration<TSut>>(mock => Mocks[position] = mock, action);
        }

        public IAppendMockConfiguration<TSut> Append(Action action)
        {
	        var position = Mocks.Count;
	        Mocks.Add(MockFactory.GetInsertMock(action, InsertMock.Location.After));
            return ConfigurationFactory.GetInsertMockConfiguration<IAppendMockConfiguration<TSut>>(mock => Mocks[position] = mock, action);
        }

        public IVerifyMockConfiguration Verify<TInput, TReturn>(Expression<Func<TInput, TReturn>> instanceSetupFunc)
            => VerifyImpl(instanceSetupFunc);

        public IVerifyMockConfiguration Verify<TInput>(Expression<Action<TInput>> voidInstanceSetupFunc)
            => VerifyImpl(voidInstanceSetupFunc);

        public IVerifyMockConfiguration Verify<TReturn>(Expression<Func<TReturn>> staticSetupFunc)
            => VerifyImpl(staticSetupFunc);

        public IVerifyMockConfiguration Verify(Expression<Action> voidStaticSetupFunc)
            => VerifyImpl(voidStaticSetupFunc);

        protected IReplaceMockConfiguration<TSut, TReturn> ReplaceImpl<TReturn>(LambdaExpression expression)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            var invocationExpression = _exprFactory(expression);
            var mock = MockFactory.GetExpressionBasedMock<ReplaceMock>(invocationExpression);
            Mocks.Add(mock);
            return ConfigurationFactory.GetReplaceMockConfiguration<IReplaceMockConfiguration<TSut, TReturn>>(mock);
        }

        protected IRemoveMockConfiguration<TSut> RemoveImpl(LambdaExpression expression)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            var invocationExpression = _exprFactory(expression);
            var mock = MockFactory.GetExpressionBasedMock<ReplaceMock>(invocationExpression);
            Mocks.Add(mock);
            return ConfigurationFactory.GetReplaceMockConfiguration<IRemoveMockConfiguration<TSut>>(mock);
        }

        protected IVerifyMockConfiguration VerifyImpl(LambdaExpression expression)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            var invocationExpression = _exprFactory(expression);
            var mock = MockFactory.GetExpressionBasedMock<VerifyMock>(invocationExpression);
            Mocks.Add(mock);
            return ConfigurationFactory.GetVerifyMockConfiguration(mock);
        }
    }
}
