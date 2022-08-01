namespace AutoFake.Abstractions
{
	public interface IExecutionContext
	{
		uint ActualCallsNumber { get; }
		CallsCheckerFunc? CallsChecker { get; }
		WhenInstanceFunc? WhenFunc { get; }
		void IncActualCalls();

		public delegate IExecutionContext Create(CallsCheckerFunc? callsChecker, WhenInstanceFunc? whenFunc);
		public delegate bool CallsCheckerFunc(uint expectedCallsCount);
		public delegate bool WhenInstanceFunc(object instance);
	}
}