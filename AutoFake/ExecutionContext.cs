using System;

namespace AutoFake
{
	public class ExecutionContext
	{
		public ExecutionContext(Func<uint, bool>? callsChecker)
		{
			CallsChecker = callsChecker;
		}

		public uint ActualCallsNumber { get; private set; }

		public Func<uint, bool>? CallsChecker { get; }

		public void IncActualCalls() => ActualCallsNumber++;
	}
}
