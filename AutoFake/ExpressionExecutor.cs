using System;

namespace AutoFake
{
    internal class ExpressionExecutor<T>
    {
	    private readonly ExpressionExecutorImpl _executor;

        public ExpressionExecutor(ExpressionExecutorImpl executor)
        {
	        _executor = executor;
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

    internal class ExpressionExecutor
    {
        private readonly ExpressionExecutorImpl _executor;

        public ExpressionExecutor(ExpressionExecutorImpl executor)
        {
            _executor = executor;
        }

        public void Execute() => _executor.Execute();
    }
}
