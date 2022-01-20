namespace AutoFake
{
	public interface IExecutionContext
	{
		uint ActualCallsNumber { get; }
		CallsCheckerFunc? CallsChecker { get; }
		void IncActualCalls();

		public delegate IExecutionContext Create(CallsCheckerFunc? callsChecker);
		public delegate bool CallsCheckerFunc(uint expectedCallsCount);
	}
}