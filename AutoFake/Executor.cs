using AutoFake.Expression;
using InvocationExpression = AutoFake.Expression.InvocationExpression;

namespace AutoFake
{
    public class Executor<T>
    {
        private readonly ExecutorImpl _executor;

        internal Executor(GeneratedObject generatedObject, InvocationExpression invocationExpression)
        {
            _executor = new ExecutorImpl(generatedObject, invocationExpression);
        }

        public T Execute() => (T)_executor.Execute();
    }

    public class Executor
    {
        private readonly ExecutorImpl _executor;

        internal Executor(GeneratedObject generatedObject, InvocationExpression invocationExpression)
        {
            _executor = new ExecutorImpl(generatedObject, invocationExpression);
        }

        public void Execute() => _executor.Execute();
    }

    internal class ExecutorImpl
    {
        private readonly GeneratedObject _generatedObject;
        private readonly InvocationExpression _invocationExpression;

        public ExecutorImpl(GeneratedObject generatedObject, InvocationExpression invocationExpression)
        {
            _generatedObject = generatedObject;
            _invocationExpression = invocationExpression;
        }

        public object Execute()
        {
            if (!_generatedObject.IsBuilt)
            {
                _generatedObject.Build();
            }

            var visitor = new GetValueMemberVisitor(_generatedObject);
            _invocationExpression.AcceptMemberVisitor(new TargetMemberVisitor(visitor, _generatedObject.Type));

            var result = visitor.RuntimeValue;

            foreach (var mockedMemberInfo in _generatedObject.MockedMembers)
            {
                mockedMemberInfo.Mock.Verify(mockedMemberInfo, _generatedObject);
            }

            return result;
        }
    }
}
