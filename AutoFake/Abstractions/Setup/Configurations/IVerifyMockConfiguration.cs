namespace AutoFake.Abstractions.Setup.Configurations;

public interface IVerifyMockConfiguration
{
	IVerifyMockConfiguration ExpectedCalls(uint expectedCallsCount);
	IVerifyMockConfiguration ExpectedCalls(IExecutionContext.CallsCheckerFunc expectedCallsCountFunc);
}
