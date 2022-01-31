using AutoFake.Setup.Mocks;
using System;
using System.Linq.Expressions;
using AutoFake.Abstractions.Setup;
using AutoFake.Abstractions.Setup.Configurations;
using AutoFake.Abstractions.Setup.Mocks;
using InvocationExpression = AutoFake.Expression.InvocationExpression;

namespace AutoFake.Setup.Configurations
{
	internal class AppendMockConfiguration<T> : AppendMockConfiguration, IAppendMockConfiguration<T>
    {
        internal AppendMockConfiguration(InvocationExpression.Create exprFactory, IMockConfigurationFactory cfgFactory,
	        IMockFactory mockFactory, Action<IMock> setMock, Action closure)
	        : base(exprFactory, cfgFactory, mockFactory, setMock, closure)
        {
        }

        public ISourceMemberInsertMockConfiguration After<TOut>(Expression<Func<T, TOut>> expression) => AfterImpl(expression);
        
        public ISourceMemberInsertMockConfiguration After(Expression<Action<T>> expression) => AfterImpl(expression);
    }

	internal class AppendMockConfiguration : IAppendMockConfiguration
    {
	    private readonly InvocationExpression.Create _exprFactory;
	    private readonly IMockConfigurationFactory _cfgFactory;
	    private readonly IMockFactory _mockFactory;
        private readonly Action<IMock> _setMock;
        private readonly Action _closure;

        internal AppendMockConfiguration(InvocationExpression.Create exprFactory, IMockConfigurationFactory cfgFactory,
	        IMockFactory mockFactory, Action<IMock> setMock, Action closure)
        {
	        _exprFactory = exprFactory;
	        _cfgFactory = cfgFactory;
	        _mockFactory = mockFactory;
            _setMock = setMock;
            _closure = closure;
        }

        public ISourceMemberInsertMockConfiguration After<TIn, TOut>(Expression<Func<TIn, TOut>> expression) => AfterImpl(expression);
        
        public ISourceMemberInsertMockConfiguration After<TIn>(Expression<Action<TIn>> expression) => AfterImpl(expression);

        public ISourceMemberInsertMockConfiguration After<TOut>(Expression<Func<TOut>> expression) => AfterImpl(expression);

        public ISourceMemberInsertMockConfiguration After(Expression<Action> expression) => AfterImpl(expression);
        
        protected ISourceMemberInsertMockConfiguration AfterImpl(LambdaExpression expression)
        {
            var mock = _mockFactory.GetSourceMemberInsertMock(_exprFactory(expression), _closure, InsertMock.Location.After);
            _setMock(mock);
            return _cfgFactory.GetSourceMemberInsertMockConfiguration(mock);
        }
    }
}
