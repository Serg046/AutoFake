using AutoFake.Abstractions;
using System;

namespace AutoFake;

internal class ExpressionExecutor<T> : IExpressionExecutor<T>
{
	private readonly IExpressionExecutorEngine _executor;

	public ExpressionExecutor(IExpressionExecutorEngine executor)
	{
		_executor = executor;
	}

	public T Execute()
	{
		try
		{
			// todo: correct
			var result = _executor.Execute();
			return result.Value != null ? (T)result.Value : default;
		}
		catch (InvalidCastException)
		{
			var typeName = typeof(T).FullName;
			throw new InvalidCastException($"Cannot cast \"this\" reference to {typeName}.");
		}
	}
}

internal class ExpressionExecutor : IExpressionExecutor
{
	private readonly IExpressionExecutorEngine _executor;

	public ExpressionExecutor(IExpressionExecutorEngine executor)
	{
		_executor = executor;
	}

	public void Execute() => _executor.Execute();
}
