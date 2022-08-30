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
			try
			{
#pragma warning disable 8603
#pragma warning disable 8600
				return (T)_executor.Execute().Value;
#pragma warning restore 8600
#pragma warning restore 8603
			}
			catch (InvalidCastException)
			{
				var typeName = typeof(T).FullName;
				throw new InvalidCastException($"Cannot cast \"this\" reference to {typeName}.");
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
