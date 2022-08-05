using System;

namespace AutoFake.Abstractions.Setup.Configurations
{
	public interface ISourceMemberInsertMockConfiguration<TSut>
	{
		ISourceMemberInsertMockConfiguration<TSut> ExpectedCalls(uint expectedCallsCount);
		ISourceMemberInsertMockConfiguration<TSut> ExpectedCalls(IExecutionContext.CallsCheckerFunc expectedCallsCountFunc);
		ISourceMemberInsertMockConfiguration<TSut> WhenArgumentsAreMatched();
		ISourceMemberInsertMockConfiguration<TSut> When(Func<bool> when);
		ISourceMemberInsertMockConfiguration<TSut> When(Func<IExecutor<TSut>, bool> when);
	}
}