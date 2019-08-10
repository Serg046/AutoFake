﻿using AutoFake.Setup.Mocks;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace AutoFake.Setup.Configurations
{
    public class MockConfiguration<T> : MockConfiguration
    {
        internal MockConfiguration(IList<IMock> mocks, IProcessorFactory processorFactory)
            : base(mocks, processorFactory)
        {
        }

        public ReplaceMockConfiguration<TReturn> Replace<TReturn>(Expression<Func<T, TReturn>> instanceSetupFunc)
            => ReplaceImpl<TReturn>(instanceSetupFunc);

        public RemoveMockConfiguration Remove(Expression<Action<T>> voidInstanceSetupFunc)
            => RemoveImpl(voidInstanceSetupFunc);

        public VerifyMockConfiguration Verify<TReturn>(Expression<Func<T, TReturn>> instanceSetupFunc)
            => VerifyImpl(instanceSetupFunc);

        public VerifyMockConfiguration Verify(Expression<Action<T>> voidInstanceSetupFunc)
            => VerifyImpl(voidInstanceSetupFunc);

        public AppendMockConfiguration<T> Append(Action<T> action)
        {
            var position = (ushort)Mocks.Count;
            var descriptor = action.ToMethodDescriptor();
            Mocks.Add(new InsertMock(ProcessorFactory, descriptor, InsertMock.Location.Bottom));
            return new AppendMockConfiguration<T>(ProcessorFactory, (mock, index) => Mocks[index] = mock,
                position, descriptor);
        }

        public PrependMockConfiguration<T> Prepend(Action<T> action)
        {
            var position = (ushort)Mocks.Count;
            var descriptor = action.ToMethodDescriptor();
            Mocks.Add(new InsertMock(ProcessorFactory, descriptor, InsertMock.Location.Top));
            return new PrependMockConfiguration<T>(ProcessorFactory, (mock, index) => Mocks[index] = mock,
                position, descriptor);
        }
    }

    public class MockConfiguration
    {
        internal MockConfiguration(IList<IMock> mocks, IProcessorFactory processorFactory)
        {
            Mocks = mocks;
            ProcessorFactory = processorFactory;
        }

        internal IList<IMock> Mocks { get; }

        internal IProcessorFactory ProcessorFactory { get; }

        protected ReplaceMockConfiguration<TReturn> ReplaceImpl<TReturn>(LambdaExpression expression)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            var invocationExpression = new Expression.InvocationExpression(expression);
            var mock = new ReplaceMock(ProcessorFactory, invocationExpression);
            Mocks.Add(mock);
            return new ReplaceMockConfiguration<TReturn>(mock);
        }

        protected RemoveMockConfiguration RemoveImpl(LambdaExpression expression)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            var invocationExpression = new Expression.InvocationExpression(expression);
            var mock = new ReplaceMock(ProcessorFactory, invocationExpression);
            Mocks.Add(mock);
            return new RemoveMockConfiguration(mock);
        }

        public ReplaceMockConfiguration<TReturn> Replace<TInput, TReturn>(Expression<Func<TInput, TReturn>> instanceSetupFunc)
            => ReplaceImpl<TReturn>(instanceSetupFunc);

        public RemoveMockConfiguration Remove<TInput>(Expression<Action<TInput>> voidInstanceSetupFunc)
            => RemoveImpl(voidInstanceSetupFunc);

        public ReplaceMockConfiguration<TReturn> Replace<TReturn>(Expression<Func<TReturn>> staticSetupFunc)
            => ReplaceImpl<TReturn>(staticSetupFunc);

        public RemoveMockConfiguration Remove(Expression<Action> voidStaticSetupFunc)
            => RemoveImpl(voidStaticSetupFunc);

        protected VerifyMockConfiguration VerifyImpl(LambdaExpression expression)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            var invocationExpression = new Expression.InvocationExpression(expression);
            var mock = new VerifyMock(ProcessorFactory, invocationExpression);
            Mocks.Add(mock);
            return new VerifyMockConfiguration(mock);
        }

        public VerifyMockConfiguration Verify<TInput, TReturn>(Expression<Func<TInput, TReturn>> instanceSetupFunc)
            => VerifyImpl(instanceSetupFunc);

        public VerifyMockConfiguration Verify<TInput>(Expression<Action<TInput>> voidInstanceSetupFunc)
            => VerifyImpl(voidInstanceSetupFunc);

        public VerifyMockConfiguration Verify<TReturn>(Expression<Func<TReturn>> staticSetupFunc)
            => VerifyImpl(staticSetupFunc);

        public VerifyMockConfiguration Verify(Expression<Action> voidStaticSetupFunc)
            => VerifyImpl(voidStaticSetupFunc);

        public AppendMockConfiguration<T> Append<T>(Action<T> action)
        {
            var position = (ushort)Mocks.Count;
            var descriptor = action.ToMethodDescriptor();
            Mocks.Add(new InsertMock(ProcessorFactory, descriptor, InsertMock.Location.Bottom));
            return new AppendMockConfiguration<T>(ProcessorFactory, (mock, index) => Mocks[index] = mock,
                position, descriptor);
        }

        public AppendMockConfiguration Append(Action action)
        {
            var position = (ushort)Mocks.Count;
            var descriptor = action.ToMethodDescriptor();
            Mocks.Add(new InsertMock(ProcessorFactory, descriptor, InsertMock.Location.Bottom));
            return new AppendMockConfiguration(ProcessorFactory, (mock, index) => Mocks[index] = mock,
                position, descriptor);
        }

        public PrependMockConfiguration<T> Prepend<T>(Action<T> action)
        {
            var position = (ushort)Mocks.Count;
            var descriptor = action.ToMethodDescriptor();
            Mocks.Add(new InsertMock(ProcessorFactory, descriptor, InsertMock.Location.Top));
            return new PrependMockConfiguration<T>(ProcessorFactory, (mock, index) => Mocks[index] = mock,
                position, descriptor);
        }

        public PrependMockConfiguration Prepend(Action action)
        {
            var position = (ushort)Mocks.Count;
            var descriptor = action.ToMethodDescriptor();
            Mocks.Add(new InsertMock(ProcessorFactory, descriptor, InsertMock.Location.Top));
            return new PrependMockConfiguration(ProcessorFactory, (mock, index) => Mocks[index] = mock,
                position, descriptor);
        }
    }
}