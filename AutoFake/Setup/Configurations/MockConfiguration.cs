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
        private readonly Executor<TReturn> _executor;

        internal FuncMockConfiguration(InvocationExpression.Create exprFactory, IMockConfigurationFactory cfgFactory,
	        IMockFactory mockFactory, IMockCollection mocks, Executor<TReturn> executor)
	        : base(exprFactory, cfgFactory, mockFactory, mocks)
        {
            _executor = executor;
        }

        public TReturn Execute() => _executor.Execute();
    }

	internal class ActionMockConfiguration<TSut> : MockConfiguration<TSut>, IActionMockConfiguration<TSut>
	{
        private readonly Executor _executor;

        internal ActionMockConfiguration(InvocationExpression.Create exprFactory, IMockConfigurationFactory cfgFactory,
	        IMockFactory mockFactory, IMockCollection mocks, Executor executor)
	        : base(exprFactory, cfgFactory, mockFactory, mocks)
        {
            _executor = executor;
        }

        public void Execute() => _executor.Execute();
    }

    internal abstract class MockConfiguration<T> : MockConfiguration
    {
        internal MockConfiguration(InvocationExpression.Create exprFactory, IMockConfigurationFactory cfgFactory,
	        IMockFactory mockFactory, IMockCollection mocks)
            : base(exprFactory, cfgFactory, mockFactory, mocks)
        {
        }

        public IReplaceMockConfiguration<TReturn> Replace<TReturn>(Expression<Func<T, TReturn>> instanceSetupFunc)
            => ReplaceImpl<TReturn>(instanceSetupFunc);

        public IRemoveMockConfiguration<T> Remove(Expression<Action<T>> voidInstanceSetupFunc)
            => RemoveImpl<T>(voidInstanceSetupFunc);

        public IRemoveMockConfiguration<T> Remove<TInput>(Expression<Action<TInput>> voidInstanceSetupFunc)
           => RemoveImpl<T>(voidInstanceSetupFunc);

        public IRemoveMockConfiguration<T> Remove(Expression<Action> voidStaticSetupFunc)
           => RemoveImpl<T>(voidStaticSetupFunc);

        public IVerifyMockConfiguration Verify<TReturn>(Expression<Func<T, TReturn>> instanceSetupFunc)
            => VerifyImpl(instanceSetupFunc);

        public IVerifyMockConfiguration Verify(Expression<Action<T>> voidInstanceSetupFunc)
            => VerifyImpl(voidInstanceSetupFunc);

        public new IPrependMockConfiguration<T> Prepend(Action action)
        {
	        var position = Mocks.Count;
	        Mocks.Add(MockFactory.GetInsertMock(action, InsertMock.Location.Before));
            return ConfigurationFactory.GetInsertMockConfiguration<IPrependMockConfiguration<T>>(mock => Mocks[position] = mock, action);
        }

        public new IAppendMockConfiguration<T> Append(Action action)
        {
	        var position = Mocks.Count;
	        Mocks.Add(MockFactory.GetInsertMock(action, InsertMock.Location.After));
            return ConfigurationFactory.GetInsertMockConfiguration<IAppendMockConfiguration<T>>(mock => Mocks[position] = mock, action);
        }
    }

    internal abstract class MockConfiguration
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

        protected IReplaceMockConfiguration<TReturn> ReplaceImpl<TReturn>(LambdaExpression expression)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            var invocationExpression = _exprFactory(expression);
            var mock = MockFactory.GetExpressionBasedMock<ReplaceMock>(invocationExpression);
            Mocks.Add(mock);
            return ConfigurationFactory.GetReplaceMockConfiguration<IReplaceMockConfiguration<TReturn>>(mock);
        }

        protected IRemoveMockConfiguration<TSut> RemoveImpl<TSut>(LambdaExpression expression)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            var invocationExpression = _exprFactory(expression);
            var mock = MockFactory.GetExpressionBasedMock<ReplaceMock>(invocationExpression);
            Mocks.Add(mock);
            return ConfigurationFactory.GetReplaceMockConfiguration<IRemoveMockConfiguration<TSut>>(mock);
        }

        public IReplaceMockConfiguration<TReturn> Replace<TInput, TReturn>(Expression<Func<TInput, TReturn>> instanceSetupFunc)
            => ReplaceImpl<TReturn>(instanceSetupFunc);

        public IReplaceMockConfiguration<TReturn> Replace<TReturn>(Expression<Func<TReturn>> staticSetupFunc)
            => ReplaceImpl<TReturn>(staticSetupFunc);

        protected IVerifyMockConfiguration VerifyImpl(LambdaExpression expression)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            var invocationExpression = _exprFactory(expression);
            var mock = MockFactory.GetExpressionBasedMock<VerifyMock>(invocationExpression);
            Mocks.Add(mock);
            return ConfigurationFactory.GetVerifyMockConfiguration(mock);
        }

        public IVerifyMockConfiguration Verify<TInput, TReturn>(Expression<Func<TInput, TReturn>> instanceSetupFunc)
            => VerifyImpl(instanceSetupFunc);

        public IVerifyMockConfiguration Verify<TInput>(Expression<Action<TInput>> voidInstanceSetupFunc)
            => VerifyImpl(voidInstanceSetupFunc);

        public IVerifyMockConfiguration Verify<TReturn>(Expression<Func<TReturn>> staticSetupFunc)
            => VerifyImpl(staticSetupFunc);

        public IVerifyMockConfiguration Verify(Expression<Action> voidStaticSetupFunc)
            => VerifyImpl(voidStaticSetupFunc);
        
        public IPrependMockConfiguration Prepend(Action action)
        {
	        var position = Mocks.Count;
            Mocks.Add(MockFactory.GetInsertMock(action, InsertMock.Location.Before));
            return ConfigurationFactory.GetInsertMockConfiguration<IPrependMockConfiguration>(mock => Mocks[position] = mock, action);
        }

        public IAppendMockConfiguration Append(Action action)
        {
	        var position = Mocks.Count;
	        Mocks.Add(MockFactory.GetInsertMock(action, InsertMock.Location.After));
	        return ConfigurationFactory.GetInsertMockConfiguration<IAppendMockConfiguration>(mock => Mocks[position] = mock, action);
        }
    }
}
