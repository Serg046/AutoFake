using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace AutoFake.Setup
{
    public class AppendMockInstaller<T> : AppendMockInstaller
    {
        internal AppendMockInstaller(IList<IMock> mocks, ushort position, MethodDescriptor action) : base(mocks, position, action)
        {
        }

        public SourceMemberInsertMockInstaller After(Expression<Action<T>> expression) => AfterImpl(expression);
    }

    public class AppendMockInstaller
    {
        private readonly IList<IMock> _mocks;
        private readonly ushort _position;
        private readonly MethodDescriptor _action;

        internal  AppendMockInstaller(IList<IMock> mocks, ushort position, MethodDescriptor action)
        {
            _mocks = mocks;
            _position = position;
            _action = action;
        }

        public SourceMemberInsertMockInstaller After<T>(Expression<Action<T>> expression) => AfterImpl(expression);

        public SourceMemberInsertMockInstaller After(Expression<Action> expression) => AfterImpl(expression);

        protected SourceMemberInsertMockInstaller AfterImpl(LambdaExpression expression)
        {
            var mock = new SourceMemberInsertMock(new Expression.InvocationExpression(expression),
                _action, InsertMock.Location.Bottom);
            _mocks[_position] = mock;
            return new SourceMemberInsertMockInstaller(mock);
        }
    }
}
