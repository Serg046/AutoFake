using System;

namespace AutoFake.Abstractions.Setup.Configurations
{
	public interface IReplaceMockConfiguration<TSut, TReturn>
	{
		IReplaceMockConfiguration<TSut, TReturn> Return(TReturn returnObject);
		IReplaceMockConfiguration<TSut, TReturn> ExpectedCalls(uint expectedCallsCount);
		IReplaceMockConfiguration<TSut, TReturn> ExpectedCalls(IExecutionContext.CallsCheckerFunc expectedCallsCountFunc);
		IReplaceMockConfiguration<TSut, TReturn> WhenArgumentsAreMatched();
		IReplaceMockConfiguration<TSut, TReturn> When(Func<IExecutor<TSut>, bool> when);
	}
}