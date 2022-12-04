using AutoFake.Abstractions;
using AutoFake.Abstractions.Setup.Configurations;
using AutoFake.Setup.Mocks;
using System;

namespace AutoFake.Setup.Configurations;

internal class SourceMemberInsertMockConfiguration<TSut> : ISourceMemberInsertMockConfiguration<TSut>
{
	private readonly SourceMemberInsertMock _mock;
	private readonly IExecutor<TSut> _executor;

	internal SourceMemberInsertMockConfiguration(SourceMemberInsertMock mock, IExecutor<TSut> executor)
	{
		_mock = mock;
		_executor = executor;
	}

	public ISourceMemberInsertMockConfiguration<TSut> ExpectedCalls(uint expectedCallsCount)
	{
		return ExpectedCalls(callsCount => callsCount == expectedCallsCount);
	}

	public ISourceMemberInsertMockConfiguration<TSut> ExpectedCalls(IExecutionContext.CallsCheckerFunc expectedCallsCountFunc)
	{
		_mock.SourceMemberMetaData.ExpectedCalls = expectedCallsCountFunc;
		return this;
	}

	public ISourceMemberInsertMockConfiguration<TSut> WhenArgumentsAreMatched()
	{
		_mock.SourceMemberMetaData.InvocationExpression.ThrowWhenArgumentsAreNotMatched = false;
		return this;
	}

	public ISourceMemberInsertMockConfiguration<TSut> When(Func<bool> when)
	{
		_mock.SourceMemberMetaData.WhenFunc = when;
		return this;
	}

	public ISourceMemberInsertMockConfiguration<TSut> When(Func<IExecutor<TSut>, bool> when)
	{
		_mock.SourceMemberMetaData.WhenFunc = () => when(_executor);
		return this;
	}
}
