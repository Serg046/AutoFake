using AutoFake.Setup.Mocks;
using System;
using System.Linq.Expressions;

namespace AutoFake.Setup.Configurations
{
    public class AppendMockConfiguration<T> : AppendMockConfiguration
    {
        internal AppendMockConfiguration(IProcessorFactory processorFactory, Action<IMock, ushort> setMock,
            ushort position, MethodDescriptor action) : base(processorFactory, setMock, position, action)
        {
        }

        public SourceMemberInsertMockConfiguration After(Expression<Action<T>> expression) => AfterImpl(expression);
    }

    public class AppendMockConfiguration
    {
        private readonly IProcessorFactory _processorFactory;
        private readonly Action<IMock, ushort> _setMock;
        private readonly ushort _position;
        private readonly MethodDescriptor _action;

        internal AppendMockConfiguration(IProcessorFactory processorFactory, Action<IMock, ushort> setMock,
            ushort position, MethodDescriptor action)
        {
            _processorFactory = processorFactory;
            _setMock = setMock;
            _position = position;
            _action = action;
        }

        public SourceMemberInsertMockConfiguration After<T>(Expression<Action<T>> expression) => AfterImpl(expression);

        public SourceMemberInsertMockConfiguration After(Expression<Action> expression) => AfterImpl(expression);

        protected SourceMemberInsertMockConfiguration AfterImpl(LambdaExpression expression)
        {
            var mock = new SourceMemberInsertMock(_processorFactory, new Expression.InvocationExpression(expression),
                _action, InsertMock.Location.Bottom);
            _setMock(mock, _position);
            return new SourceMemberInsertMockConfiguration(mock);
        }
    }
}
