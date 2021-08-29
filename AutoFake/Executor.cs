using System;
using System.Reflection;
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

        public T Execute()
        {
	        var visitor = _executor.Execute();
            try
            {
#pragma warning disable 8603
#pragma warning disable 8600
	            return (T)visitor.RuntimeValue;
#pragma warning restore 8600
#pragma warning restore 8603
            }
            catch (InvalidCastException)
            {
	            var type = typeof(T);
	            if (type.Module.Assembly != visitor.Type.Module.Assembly)
	            {
		            throw new InvalidCastException("The executable member must be processed by Rewrite() method");
                }

	            var typeName = type.FullName;
	            throw new InvalidCastException($"Cannot cast \"this\" reference to {typeName}. Consider executing some member of {typeName}.");
            }
        }
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

        public GetValueMemberVisitor Execute()
        {
            var fakeObject = _fake.GetFakeObject();
            var visitor = new GetValueMemberVisitor(fakeObject.Instance);
            try
            {
	            _invocationExpression.AcceptMemberVisitor(new TargetMemberVisitor(visitor, fakeObject.SourceType));
	            return visitor;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
	            throw ex.InnerException;
            }
        }
    }
}
