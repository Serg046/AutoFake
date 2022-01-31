using AutoFake.Setup.Mocks;
using System;
using System.Linq.Expressions;
using AutoFake.Abstractions.Setup;
using AutoFake.Abstractions.Setup.Configurations;
using AutoFake.Abstractions.Setup.Mocks;
using InvocationExpression = AutoFake.Expression.InvocationExpression;

namespace AutoFake.Setup.Configurations
{
	internal class PrependMockConfiguration<T> : PrependMockConfiguration, IPrependMockConfiguration<T>
    {
        internal PrependMockConfiguration(InvocationExpression.Create exprFactory, IMockConfigurationFactory cfgFactory,
	        IMockFactory mockFactory, Action<IMock> setMock, Action closure)
	        : base(exprFactory, cfgFactory, mockFactory, setMock, closure)
        {
        }

        public ISourceMemberInsertMockConfiguration Before<TOut>(Expression<Func<T, TOut>> expression) => BeforeImpl(expression);
        
        public ISourceMemberInsertMockConfiguration Before(Expression<Action<T>> expression) => BeforeImpl(expression);
    }

	internal class PrependMockConfiguration : IPrependMockConfiguration
    {
	    private readonly InvocationExpression.Create _exprFactory;
	    private readonly IMockConfigurationFactory _cfgFactory;
	    private readonly IMockFactory _mockFactory;
        private readonly Action<IMock> _setMock;
        private readonly Action _closure;

        internal PrependMockConfiguration(InvocationExpression.Create exprFactory, IMockConfigurationFactory cfgFactory,
	        IMockFactory mockFactory, Action<IMock> setMock, Action closure)
        {
	        _exprFactory = exprFactory;
	        _cfgFactory = cfgFactory;
	        _mockFactory = mockFactory;
            _setMock = setMock;
            _closure = closure;
        }

        public ISourceMemberInsertMockConfiguration Before<TIn, TOut>(Expression<Func<TIn, TOut>> expression) => BeforeImpl(expression);
        
        public ISourceMemberInsertMockConfiguration Before<TIn>(Expression<Action<TIn>> expression) => BeforeImpl(expression);

        public ISourceMemberInsertMockConfiguration Before<TOut>(Expression<Func<TOut>> expression) => BeforeImpl(expression);
        
        public ISourceMemberInsertMockConfiguration Before(Expression<Action> expression) => BeforeImpl(expression);
        

        protected ISourceMemberInsertMockConfiguration BeforeImpl(LambdaExpression expression)
        {
            var mock = _mockFactory.GetSourceMemberInsertMock(_exprFactory(expression), _closure, InsertMock.Location.Before);
            _setMock(mock);
            return _cfgFactory.GetSourceMemberInsertMockConfiguration(mock);
        }
    }
}
