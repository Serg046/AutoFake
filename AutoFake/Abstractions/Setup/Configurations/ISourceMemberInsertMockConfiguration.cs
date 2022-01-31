namespace AutoFake.Abstractions.Setup.Configurations
{
	public interface ISourceMemberInsertMockConfiguration
	{
		ISourceMemberInsertMockConfiguration ExpectedCalls(uint expectedCallsCount);
		ISourceMemberInsertMockConfiguration ExpectedCalls(IExecutionContext.CallsCheckerFunc expectedCallsCountFunc);
		ISourceMemberInsertMockConfiguration WhenArgumentsAreMatched();
	}
}