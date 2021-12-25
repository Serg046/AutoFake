using AutoFake.Setup.Mocks;
using System;
using System.Linq.Expressions;
using InvocationExpression = AutoFake.Expression.InvocationExpression;

namespace AutoFake.Setup.Configurations
{
    public class FuncMockConfiguration<TSut, TReturn> : MockConfiguration<TSut>
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

    public class ActionMockConfiguration<TSut> : MockConfiguration<TSut>
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

    public class FuncMockConfiguration<TReturn> : MockConfiguration
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

    public class ActionMockConfiguration : MockConfiguration
    {
        private readonly Executor _executor;

        internal ActionMockConfiguration(InvocationExpression.Create exprFactory, IMockConfigurationFactory cfgFactory,
	        IMockFactory mockFactory, IMockCollection mocks, Executor executor)
	        : base(exprFactory,cfgFactory, mockFactory, mocks)
        {
            _executor = executor;
        }

        public void Execute() => _executor.Execute();
    }

    public abstract class MockConfiguration<T> : MockConfiguration
    {
        internal MockConfiguration(InvocationExpression.Create exprFactory, IMockConfigurationFactory cfgFactory,
	        IMockFactory mockFactory, IMockCollection mocks)
            : base(exprFactory, cfgFactory, mockFactory, mocks)
        {
        }

        public ReplaceMockConfiguration<TReturn> Replace<TReturn>(Expression<Func<T, TReturn>> instanceSetupFunc)
            => ReplaceImpl<TReturn>(instanceSetupFunc);

        public RemoveMockConfiguration Remove(Expression<Action<T>> voidInstanceSetupFunc)
            => RemoveImpl(voidInstanceSetupFunc);

        public VerifyMockConfiguration Verify<TReturn>(Expression<Func<T, TReturn>> instanceSetupFunc)
            => VerifyImpl(instanceSetupFunc);

        public VerifyMockConfiguration Verify(Expression<Action<T>> voidInstanceSetupFunc)
            => VerifyImpl(voidInstanceSetupFunc);

        public new PrependMockConfiguration<T> Prepend(Action action)
        {
	        var position = Mocks.Count;
	        Mocks.Add(MockFactory.GetInsertMock(action, InsertMock.Location.Before));
            return ConfigurationFactory.GetPrependMockConfiguration<PrependMockConfiguration<T>>(mock => Mocks[position] = mock, action);
        }

        public new AppendMockConfiguration<T> Append(Action action)
        {
	        var position = Mocks.Count;
	        Mocks.Add(MockFactory.GetInsertMock(action, InsertMock.Location.After));
            return ConfigurationFactory.GetAppendMockConfiguration<AppendMockConfiguration<T>>(mock => Mocks[position] = mock, action);
        }
    }

    public abstract class MockConfiguration
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

        protected ReplaceMockConfiguration<TReturn> ReplaceImpl<TReturn>(LambdaExpression expression)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            var invocationExpression = _exprFactory(expression);
            var mock = MockFactory.GetExpressionBasedMock<ReplaceMock>(invocationExpression);
            Mocks.Add(mock);
            return ConfigurationFactory.GetReplaceMockConfiguration<ReplaceMockConfiguration<TReturn>>(mock);
        }

        protected RemoveMockConfiguration RemoveImpl(LambdaExpression expression)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            var invocationExpression = _exprFactory(expression);
            var mock = MockFactory.GetExpressionBasedMock<ReplaceMock>(invocationExpression);
            Mocks.Add(mock);
            return ConfigurationFactory.GetReplaceMockConfiguration<RemoveMockConfiguration>(mock);
        }

        public ReplaceMockConfiguration<TReturn> Replace<TInput, TReturn>(Expression<Func<TInput, TReturn>> instanceSetupFunc)
            => ReplaceImpl<TReturn>(instanceSetupFunc);

        public RemoveMockConfiguration Remove<TInput>(Expression<Action<TInput>> voidInstanceSetupFunc)
            => RemoveImpl(voidInstanceSetupFunc);

        public ReplaceMockConfiguration<TReturn> Replace<TReturn>(Expression<Func<TReturn>> staticSetupFunc)
            => ReplaceImpl<TReturn>(staticSetupFunc);

        public RemoveMockConfiguration Remove(Expression<Action> voidStaticSetupFunc)
            => RemoveImpl(voidStaticSetupFunc);

        protected VerifyMockConfiguration VerifyImpl(LambdaExpression expression)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            var invocationExpression = _exprFactory(expression);
            var mock = MockFactory.GetExpressionBasedMock<VerifyMock>(invocationExpression);
            Mocks.Add(mock);
            return ConfigurationFactory.GetVerifyMockConfiguration(mock);
        }

        public VerifyMockConfiguration Verify<TInput, TReturn>(Expression<Func<TInput, TReturn>> instanceSetupFunc)
            => VerifyImpl(instanceSetupFunc);

        public VerifyMockConfiguration Verify<TInput>(Expression<Action<TInput>> voidInstanceSetupFunc)
            => VerifyImpl(voidInstanceSetupFunc);

        public VerifyMockConfiguration Verify<TReturn>(Expression<Func<TReturn>> staticSetupFunc)
            => VerifyImpl(staticSetupFunc);

        public VerifyMockConfiguration Verify(Expression<Action> voidStaticSetupFunc)
            => VerifyImpl(voidStaticSetupFunc);
        
        public PrependMockConfiguration Prepend(Action action)
        {
	        var position = Mocks.Count;
            Mocks.Add(MockFactory.GetInsertMock(action, InsertMock.Location.Before));
            return ConfigurationFactory.GetPrependMockConfiguration<PrependMockConfiguration>(mock => Mocks[position] = mock, action);
        }

        public AppendMockConfiguration Append(Action action)
        {
	        var position = Mocks.Count;
	        Mocks.Add(MockFactory.GetInsertMock(action, InsertMock.Location.After));
	        return ConfigurationFactory.GetAppendMockConfiguration<AppendMockConfiguration>(mock => Mocks[position] = mock, action);
        }
    }
}
