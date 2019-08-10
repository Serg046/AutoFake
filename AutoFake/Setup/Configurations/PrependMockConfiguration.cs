using AutoFake.Setup.Mocks;
using System;
using System.Linq.Expressions;

namespace AutoFake.Setup.Configurations
{
    public class PrependMockConfiguration<T> : PrependMockConfiguration
    {
        internal PrependMockConfiguration(IProcessorFactory processorFactory, Action<IMock, ushort> setMock,
            ushort position, MethodDescriptor action) : base(processorFactory, setMock, position, action)
        {
        }

        public SourceMemberInsertMockConfiguration Before(Expression<Action<T>> expression) => BeforeImpl(expression);
    }

    public class PrependMockConfiguration
    {
        private readonly IProcessorFactory _processorFactory;
        private readonly Action<IMock, ushort> _setMock;
        private readonly ushort _position;
        private readonly MethodDescriptor _action;

        internal PrependMockConfiguration(IProcessorFactory processorFactory, Action<IMock, ushort> setMock,
            ushort position, MethodDescriptor action)
        {
            _processorFactory = processorFactory;
            _setMock = setMock;
            _position = position;
            _action = action;
        }

        public SourceMemberInsertMockConfiguration Before<T>(Expression<Action<T>> expression) => BeforeImpl(expression);

        public SourceMemberInsertMockConfiguration Before(Expression<Action> expression) => BeforeImpl(expression);

        protected SourceMemberInsertMockConfiguration BeforeImpl(LambdaExpression expression)
        {
            var mock = new SourceMemberInsertMock(_processorFactory, new Expression.InvocationExpression(expression),
                _action, InsertMock.Location.Top);
            _setMock(mock, _position);
            return new SourceMemberInsertMockConfiguration(mock);
        }
    }
}
