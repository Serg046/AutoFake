using System;

namespace AutoFake.Abstractions;

public interface IExpressionExecutorEngine
{
	(Type Type, object? Value) Execute();
}
