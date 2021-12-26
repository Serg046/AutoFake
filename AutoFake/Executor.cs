using System;
using System.Reflection;
using AutoFake.Expression;

namespace AutoFake
{
    internal class Executor<T>
    {
	    private readonly ExecutorImpl _executor;

        public Executor(Fake fake, IInvocationExpression invocationExpression, IMemberVisitorFactory memberVisitorFactory)
        {
	        _executor = new ExecutorImpl(fake, invocationExpression, memberVisitorFactory);
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

        public Executor(Fake fake, IInvocationExpression invocationExpression, IMemberVisitorFactory memberVisitorFactory)
        {
            _executor = new ExecutorImpl(fake, invocationExpression, memberVisitorFactory);
        }

        public void Execute() => _executor.Execute();
    }

    internal class ExecutorImpl
    {
        private readonly Fake _fake;
        private readonly IInvocationExpression _invocationExpression;
        private readonly IMemberVisitorFactory _memberVisitorFactory;

        public ExecutorImpl(Fake fake, IInvocationExpression invocationExpression, IMemberVisitorFactory memberVisitorFactory)
        {
            _fake = fake;
            _invocationExpression = invocationExpression;
            _memberVisitorFactory = memberVisitorFactory;
        }

        public GetValueMemberVisitor Execute()
        {
            var fakeObject = _fake.GetFakeObject();
            var visitor = _memberVisitorFactory.GetValueMemberVisitor(fakeObject.Instance);
            try
            {
	            _invocationExpression.AcceptMemberVisitor(_memberVisitorFactory.GetTargetMemberVisitor(visitor, fakeObject.SourceType));
	            return visitor;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
	            throw ex.InnerException;
            }
        }
    }
}
