using System;
using AutoFake.Expression;

namespace AutoFake
{
    internal class Executor<T>
    {
        private readonly ExecutorImpl _executor;

        internal Executor(Fake fake, IInvocationExpression invocationExpression)
        {
            _executor = new ExecutorImpl(fake, invocationExpression);
        }

        public T Execute() => (T)_executor.Execute();
    }

    internal class Executor
    {
        private readonly ExecutorImpl _executor;

        internal Executor(Fake fake, IInvocationExpression invocationExpression)
        {
            _executor = new ExecutorImpl(fake, invocationExpression);
        }

        public void Execute() => _executor.Execute();
    }

    internal class ExecutorImpl
    {
        private readonly Fake _fake;
        private readonly IInvocationExpression _invocationExpression;

        public ExecutorImpl(Fake fake, IInvocationExpression invocationExpression)
        {
            _fake = fake;
            _invocationExpression = invocationExpression;
        }

        public object Execute()
        {
            var fakeObject = _fake.CreateFakeObject();
            if (fakeObject.IsDisposed)
            {
	            throw new ObjectDisposedException(
		            $"The fake assembly has been already unloaded. Use fake.{nameof(Fake.Options)}.{nameof(Fake.Options.AutoDisposal)}/fake.{nameof(Fake.Release)}() to get control.");
            }

            var visitor = new GetValueMemberVisitor(fakeObject.Instance);
            _invocationExpression.AcceptMemberVisitor(new TargetMemberVisitor(visitor, fakeObject.Type));
            if (_fake.Options.AutoDisposal) fakeObject.Dispose();
            return visitor.RuntimeValue;
        }
    }
}
