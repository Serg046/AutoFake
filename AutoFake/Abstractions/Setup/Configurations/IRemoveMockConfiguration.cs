using System;

namespace AutoFake.Abstractions.Setup.Configurations
{
	public interface IRemoveMockConfiguration<TSut>
	{
		IRemoveMockConfiguration<TSut> ExpectedCalls(uint expectedCallsCount);
		IRemoveMockConfiguration<TSut> ExpectedCalls(IExecutionContext.CallsCheckerFunc expectedCallsCountFunc);
		IRemoveMockConfiguration<TSut> WhenArgumentsAreMatched();
		IRemoveMockConfiguration<TSut> When(Func<bool> when);
		IRemoveMockConfiguration<TSut> When(Func<IExecutor<TSut>, bool> when);
	}
}