namespace AutoFake.Abstractions;

public interface IExpressionExecutor<T>
{
	T Execute();
}

public interface IExpressionExecutor
{
	void Execute();
}
