using AutoFake.Setup.Mocks;
using System;
using System.Linq.Expressions;
using AutoFake.Abstractions.Setup;
using AutoFake.Abstractions.Setup.Configurations;
using AutoFake.Abstractions.Setup.Mocks;
using InvocationExpression = AutoFake.Expression.InvocationExpression;

namespace AutoFake.Setup.Configurations
{
	internal class PrependMockConfiguration<TSut> : IPrependMockConfiguration<TSut>
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

        public ISourceMemberInsertMockConfiguration<TSut> Before<TOut>(Expression<Func<TSut, TOut>> expression) => BeforeImpl(expression);
        
        public ISourceMemberInsertMockConfiguration<TSut> Before(Expression<Action<TSut>> expression) => BeforeImpl(expression);

        public ISourceMemberInsertMockConfiguration<TSut> Before<TIn, TOut>(Expression<Func<TIn, TOut>> expression) => BeforeImpl(expression);
        
        public ISourceMemberInsertMockConfiguration<TSut> Before<TIn>(Expression<Action<TIn>> expression) => BeforeImpl(expression);

        public ISourceMemberInsertMockConfiguration<TSut> Before<TOut>(Expression<Func<TOut>> expression) => BeforeImpl(expression);
        
        public ISourceMemberInsertMockConfiguration<TSut> Before(Expression<Action> expression) => BeforeImpl(expression);
        

        protected ISourceMemberInsertMockConfiguration<TSut> BeforeImpl(LambdaExpression expression)
        {
            var mock = _mockFactory.GetSourceMemberInsertMock(_exprFactory(expression), _closure, InsertMock.Location.Before);
            _setMock(mock);
            return _cfgFactory.GetSourceMemberInsertMockConfiguration<TSut>(mock);
        }
    }
}
