using AutoFake.Abstractions;

namespace AutoFake
{
	internal class ExecutionContext : IExecutionContext
	{
		public ExecutionContext(IExecutionContext.CallsCheckerFunc? callsChecker)
		{
			CallsChecker = callsChecker;
		}

		public uint ActualCallsNumber { get; private set; }

		public IExecutionContext.CallsCheckerFunc? CallsChecker { get; }

		public void IncActualCalls() => ActualCallsNumber++;
	}
}
