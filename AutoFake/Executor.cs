﻿using AutoFake.Expression;

namespace AutoFake
{
    internal class Executor<T>
    {
        private readonly ExecutorImpl _executor;

        internal Executor(Fake fake, InvocationExpression invocationExpression)
        {
            _executor = new ExecutorImpl(fake, invocationExpression);
        }

        public T Execute() => (T)_executor.Execute();
    }

    internal class Executor
    {
        private readonly ExecutorImpl _executor;

        internal Executor(Fake fake, InvocationExpression invocationExpression)
        {
            _executor = new ExecutorImpl(fake, invocationExpression);
        }

        public void Execute() => _executor.Execute();
    }

    internal class ExecutorImpl
    {
        private readonly Fake _fake;
        private readonly InvocationExpression _invocationExpression;

        public ExecutorImpl(Fake fake, InvocationExpression invocationExpression)
        {
            _fake = fake;
            _invocationExpression = invocationExpression;
        }

        public object Execute()
        {
            var fakeObject = _fake.CreateFakeObject();
            var visitor = new GetValueMemberVisitor(fakeObject.Instance);
            _invocationExpression.AcceptMemberVisitor(new TargetMemberVisitor(visitor, fakeObject.Type));
            var result = visitor.RuntimeValue;
            return result;
        }
    }
}