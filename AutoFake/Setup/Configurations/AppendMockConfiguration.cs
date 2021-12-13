using AutoFake.Setup.Mocks;
using System;
using System.Linq.Expressions;

namespace AutoFake.Setup.Configurations
{
    public class AppendMockConfiguration<T> : AppendMockConfiguration
    {
        internal AppendMockConfiguration(IProcessorFactory processorFactory, Action<IMock> setMock, Action closure) : base(processorFactory, setMock, closure)
        {
        }

        public SourceMemberInsertMockConfiguration After<TOut>(Expression<Func<T, TOut>> expression) => AfterImpl(expression);
        
        public SourceMemberInsertMockConfiguration After(Expression<Action<T>> expression) => AfterImpl(expression);
    }

    public class AppendMockConfiguration
    {
        private readonly IProcessorFactory _processorFactory;
        private readonly Action<IMock> _setMock;
        private readonly Action _closure;

        internal AppendMockConfiguration(IProcessorFactory processorFactory, Action<IMock> setMock, Action closure)
        {
            _processorFactory = processorFactory;
            _setMock = setMock;
            _closure = closure;
        }

        public SourceMemberInsertMockConfiguration After<TIn, TOut>(Expression<Func<TIn, TOut>> expression) => AfterImpl(expression);
        
        public SourceMemberInsertMockConfiguration After<TIn>(Expression<Action<TIn>> expression) => AfterImpl(expression);

        public SourceMemberInsertMockConfiguration After<TOut>(Expression<Func<TOut>> expression) => AfterImpl(expression);

        public SourceMemberInsertMockConfiguration After(Expression<Action> expression) => AfterImpl(expression);
        
        protected SourceMemberInsertMockConfiguration AfterImpl(LambdaExpression expression)
        {
            var mock = new SourceMemberInsertMock(_processorFactory, new Expression.InvocationExpression(expression),
                _closure, InsertMock.Location.After);
            _setMock(mock);
            return new SourceMemberInsertMockConfiguration(mock);
        }
    }
}
