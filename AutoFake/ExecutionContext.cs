using AutoFake.Abstractions;
using System;

namespace AutoFake;

internal class ExecutionContext : IExecutionContext
{
	public ExecutionContext(IExecutionContext.CallsCheckerFunc? callsChecker, Func<bool>? whenFunc)
	{
		CallsChecker = callsChecker;
		WhenFunc = whenFunc;
	}

	public uint ActualCallsNumber { get; private set; }

	public IExecutionContext.CallsCheckerFunc? CallsChecker { get; }

	public Func<bool>? WhenFunc { get; }

	public void IncActualCalls() => ActualCallsNumber++;
}
