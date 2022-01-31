namespace AutoFake.Abstractions.Setup.Configurations
{
	public interface IRemoveMockConfiguration
	{
		IRemoveMockConfiguration ExpectedCalls(uint expectedCallsCount);
		IRemoveMockConfiguration ExpectedCalls(IExecutionContext.CallsCheckerFunc expectedCallsCountFunc);
		IRemoveMockConfiguration WhenArgumentsAreMatched();
	}
}