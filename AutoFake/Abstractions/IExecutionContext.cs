using System;

namespace AutoFake.Abstractions
{
	public interface IExecutionContext
	{
		uint ActualCallsNumber { get; }
		CallsCheckerFunc? CallsChecker { get; }
		Func<bool>? WhenFunc { get; }
		void IncActualCalls();

		public delegate IExecutionContext Create(CallsCheckerFunc? callsChecker, Func<bool>? whenFunc);
		public delegate bool CallsCheckerFunc(uint expectedCallsCount);
	}
}