using AutoFake.Abstractions;
using AutoFake.Abstractions.Setup.Configurations;
using AutoFake.Setup.Mocks;
using System;

namespace AutoFake.Setup.Configurations
{
	internal class RemoveMockConfiguration<TSut> : IRemoveMockConfiguration<TSut>
	{
		private readonly ReplaceMock _mock;
		private readonly IExecutor<TSut> _executor;

		internal RemoveMockConfiguration(ReplaceMock mock, IExecutor<TSut> executor)
		{
			_mock = mock;
			_executor = executor;
		}

		public IRemoveMockConfiguration<TSut> ExpectedCalls(uint expectedCallsCount)
		{
			return ExpectedCalls(callsCount => callsCount == expectedCallsCount);
		}

		public IRemoveMockConfiguration<TSut> ExpectedCalls(IExecutionContext.CallsCheckerFunc expectedCallsCountFunc)
		{
			_mock.SourceMemberMetaData.ExpectedCalls = expectedCallsCountFunc;
			return this;
		}

		public IRemoveMockConfiguration<TSut> WhenArgumentsAreMatched()
		{
			_mock.SourceMemberMetaData.InvocationExpression.ThrowWhenArgumentsAreNotMatched = false;
			return this;
		}

		public IRemoveMockConfiguration<TSut> When(Func<bool> when)
		{
			_mock.SourceMemberMetaData.WhenFunc = when;
			return this;
		}

		public IRemoveMockConfiguration<TSut> When(Func<IExecutor<TSut>, bool> when)
		{
			_mock.SourceMemberMetaData.WhenFunc = () => when(_executor);
			return this;
		}
	}
}
