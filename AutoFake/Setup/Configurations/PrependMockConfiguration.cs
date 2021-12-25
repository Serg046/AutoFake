using AutoFake.Setup.Mocks;
using System;
using System.Linq.Expressions;
using InvocationExpression = AutoFake.Expression.InvocationExpression;

namespace AutoFake.Setup.Configurations
{
    public class PrependMockConfiguration<T> : PrependMockConfiguration
    {
        internal PrependMockConfiguration(InvocationExpression.Create exprFactory, IMockConfigurationFactory cfgFactory,
	        IMockFactory mockFactory, Action<IMock> setMock, Action closure)
	        : base(exprFactory, cfgFactory, mockFactory, setMock, closure)
        {
        }

        public SourceMemberInsertMockConfiguration Before<TOut>(Expression<Func<T, TOut>> expression) => BeforeImpl(expression);
        
        public SourceMemberInsertMockConfiguration Before(Expression<Action<T>> expression) => BeforeImpl(expression);
    }

    public class PrependMockConfiguration
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

        public SourceMemberInsertMockConfiguration Before<TIn, TOut>(Expression<Func<TIn, TOut>> expression) => BeforeImpl(expression);
        
        public SourceMemberInsertMockConfiguration Before<TIn>(Expression<Action<TIn>> expression) => BeforeImpl(expression);

        public SourceMemberInsertMockConfiguration Before<TOut>(Expression<Func<TOut>> expression) => BeforeImpl(expression);
        
        public SourceMemberInsertMockConfiguration Before(Expression<Action> expression) => BeforeImpl(expression);
        

        protected SourceMemberInsertMockConfiguration BeforeImpl(LambdaExpression expression)
        {
            var mock = _mockFactory.GetSourceMemberInsertMock(_exprFactory(expression), _closure, InsertMock.Location.Before);
            _setMock(mock);
            return _cfgFactory.GetSourceMemberInsertMockConfiguration(mock);
        }
    }
}
