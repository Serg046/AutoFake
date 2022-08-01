using AutoFake.Abstractions;

namespace AutoFake
{
	internal class ExecutionContext : IExecutionContext
	{
		public ExecutionContext(IExecutionContext.CallsCheckerFunc? callsChecker, IExecutionContext.WhenInstanceFunc? whenFunc)
		{
			CallsChecker = callsChecker;
			WhenFunc = whenFunc;
		}

		public uint ActualCallsNumber { get; private set; }

		public IExecutionContext.CallsCheckerFunc? CallsChecker { get; }

		public IExecutionContext.WhenInstanceFunc? WhenFunc { get; }

		public void IncActualCalls() => ActualCallsNumber++;
	}
}
