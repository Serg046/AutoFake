using AutoFake.Setup.Mocks;
using System;
using System.Linq.Expressions;

namespace AutoFake.Setup.Configurations
{
    public class AppendMockConfiguration<T> : AppendMockConfiguration
    {
        internal AppendMockConfiguration(IProcessorFactory processorFactory, Action<IMock, ushort> setMock,
            ushort position, ClosureDescriptor closure) : base(processorFactory, setMock, position, closure)
        {
        }

        public SourceMemberInsertMockConfiguration After(Expression<Action<T>> expression) => AfterImpl(expression);
    }

    public class AppendMockConfiguration
    {
        private readonly IProcessorFactory _processorFactory;
        private readonly Action<IMock, ushort> _setMock;
        private readonly ushort _position;
        private readonly ClosureDescriptor _closure;

        internal AppendMockConfiguration(IProcessorFactory processorFactory, Action<IMock, ushort> setMock,
            ushort position, ClosureDescriptor closure)
        {
            _processorFactory = processorFactory;
            _setMock = setMock;
            _position = position;
            _closure = closure;
        }

        public SourceMemberInsertMockConfiguration After<T>(Expression<Action<T>> expression) => AfterImpl(expression);

        public SourceMemberInsertMockConfiguration After(Expression<Action> expression) => AfterImpl(expression);

        protected SourceMemberInsertMockConfiguration AfterImpl(LambdaExpression expression)
        {
            var mock = new SourceMemberInsertMock(_processorFactory, new Expression.InvocationExpression(expression),
                _closure, InsertMock.Location.Bottom);
            _setMock(mock, _position);
            return new SourceMemberInsertMockConfiguration(mock, _processorFactory);
        }
    }
}
