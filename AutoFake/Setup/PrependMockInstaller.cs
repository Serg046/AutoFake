using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace AutoFake.Setup
{
    public class PrependMockInstaller<T> : PrependMockInstaller
    {
        internal PrependMockInstaller(IList<IMock> mocks, ushort position, MethodDescriptor action) : base(mocks, position, action)
        {
        }

        public SourceMemberInsertMockInstaller Before(Expression<Action<T>> expression) => BeforeImpl(expression);
    }

    public class PrependMockInstaller
    {
        private readonly IList<IMock> _mocks;
        private readonly ushort _position;
        private readonly MethodDescriptor _action;

        internal PrependMockInstaller(IList<IMock> mocks, ushort position, MethodDescriptor action)
        {
            _mocks = mocks;
            _position = position;
            _action = action;
        }

        public SourceMemberInsertMockInstaller Before<T>(Expression<Action<T>> expression) => BeforeImpl(expression);

        public SourceMemberInsertMockInstaller Before(Expression<Action> expression) => BeforeImpl(expression);

        protected SourceMemberInsertMockInstaller BeforeImpl(LambdaExpression expression)
        {
            var mock = new SourceMemberInsertMock(new Expression.InvocationExpression(expression),
                _action, InsertMock.Location.Top);
            _mocks[_position] = mock;
            return new SourceMemberInsertMockInstaller(mock);
        }
    }
}
