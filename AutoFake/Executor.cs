using System.Linq.Expressions;

namespace AutoFake
{
    public class Executor<T>
    {
        private readonly ExecutorImpl _executor;

        internal Executor(GeneratedObject generatedObject, LambdaExpression invocationExpression)
        {
            _executor = new ExecutorImpl(generatedObject, invocationExpression);
        }

        public T Execute() => (T)_executor.Execute();
    }

    public class Executor
    {
        private readonly ExecutorImpl _executor;

        internal Executor(GeneratedObject generatedObject, LambdaExpression invocationExpression)
        {
            _executor = new ExecutorImpl(generatedObject, invocationExpression);
        }

        public void Execute() => _executor.Execute();
    }

    internal class ExecutorImpl
    {
        private readonly GeneratedObject _generatedObject;
        private readonly LambdaExpression _invocationExpression;

        public ExecutorImpl(GeneratedObject generatedObject, LambdaExpression invocationExpression)
        {
            _generatedObject = generatedObject;
            _invocationExpression = invocationExpression;
        }

        public object Execute()
        {
            if (!_generatedObject.IsBuilt)
            {
                _generatedObject.Build();
                InitializeInstanceState();
                
            }
            var visitor = new GetValueMemberVisitor(_generatedObject);
            _generatedObject.AcceptMemberVisitor(_invocationExpression.Body, visitor);

            var result = visitor.RuntimeValue;

            foreach (var mockedMemberInfo in _generatedObject.MockedMembers)
            {
                mockedMemberInfo.Mock.Verify(mockedMemberInfo, _generatedObject);
            }

            return result;
        }

        private void InitializeInstanceState()
        {
            foreach (var mockedMemberInfo in _generatedObject.MockedMembers)
            {
                mockedMemberInfo.Mock.Initialize(mockedMemberInfo, _generatedObject);
            }
        }
    }
}
