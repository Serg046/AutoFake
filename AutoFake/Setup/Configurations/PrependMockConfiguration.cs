﻿using AutoFake.Setup.Mocks;
using System;
using System.Linq.Expressions;

namespace AutoFake.Setup.Configurations
{
    public class PrependMockConfiguration<T> : PrependMockConfiguration
    {
        internal PrependMockConfiguration(IProcessorFactory processorFactory, Action<IMock, ushort> setMock,
            ushort position, ClosureDescriptor closure) : base(processorFactory, setMock, position, closure)
        {
        }

        public SourceMemberInsertMockConfiguration Before(Expression<Action<T>> expression) => BeforeImpl(expression);
    }

    public class PrependMockConfiguration
    {
        private readonly IProcessorFactory _processorFactory;
        private readonly Action<IMock, ushort> _setMock;
        private readonly ushort _position;
        private readonly ClosureDescriptor _closure;

        internal PrependMockConfiguration(IProcessorFactory processorFactory, Action<IMock, ushort> setMock,
            ushort position, ClosureDescriptor closure)
        {
            _processorFactory = processorFactory;
            _setMock = setMock;
            _position = position;
            _closure = closure;
        }

        public SourceMemberInsertMockConfiguration Before<T>(Expression<Action<T>> expression) => BeforeImpl(expression);

        public SourceMemberInsertMockConfiguration Before(Expression<Action> expression) => BeforeImpl(expression);

        protected SourceMemberInsertMockConfiguration BeforeImpl(LambdaExpression expression)
        {
            var mock = new SourceMemberInsertMock(_processorFactory, new Expression.InvocationExpression(expression),
                _closure, InsertMock.Location.Top);
            _setMock(mock, _position);
            return new SourceMemberInsertMockConfiguration(mock, _processorFactory);
        }
    }
}
