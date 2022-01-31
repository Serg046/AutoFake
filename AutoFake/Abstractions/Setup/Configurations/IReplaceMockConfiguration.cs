namespace AutoFake.Abstractions.Setup.Configurations
{
	public interface IReplaceMockConfiguration<TReturn>
	{
		IReplaceMockConfiguration<TReturn> Return(TReturn returnObject);
		IReplaceMockConfiguration<TReturn> ExpectedCalls(uint expectedCallsCount);
		IReplaceMockConfiguration<TReturn> ExpectedCalls(IExecutionContext.CallsCheckerFunc expectedCallsCountFunc);
		IReplaceMockConfiguration<TReturn> WhenArgumentsAreMatched();
	}
}