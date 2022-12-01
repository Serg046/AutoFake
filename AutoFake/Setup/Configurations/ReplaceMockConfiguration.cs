using AutoFake.Setup.Mocks;
using System;
using AutoFake.Abstractions;
using AutoFake.Abstractions.Setup.Configurations;

namespace AutoFake.Setup.Configurations
{
	internal class ReplaceMockConfiguration<TSut, TReturn> : IReplaceMockConfiguration<TSut, TReturn>
	{
		private readonly ReplaceMock _mock;
		private readonly IExecutor<TSut> _executor;

		internal ReplaceMockConfiguration(ReplaceMock mock, IExecutor<TSut> executor)
		{
			_mock = mock;
			_executor = executor;
		}

		public IReplaceMockConfiguration<TSut, TReturn> Return(TReturn returnObject)
		{
			_mock.ReturnObject = returnObject;
			return this;
		}

		public IReplaceMockConfiguration<TSut, TReturn> ExpectedCalls(uint expectedCallsCount)
		{
			return ExpectedCalls(callsCount => callsCount == expectedCallsCount);
		}

		public IReplaceMockConfiguration<TSut, TReturn> ExpectedCalls(IExecutionContext.CallsCheckerFunc expectedCallsCountFunc)
		{
			_mock.SourceMemberMetaData.ExpectedCalls = expectedCallsCountFunc;
			return this;
		}

		public IReplaceMockConfiguration<TSut, TReturn> WhenArgumentsAreMatched()
		{
			_mock.SourceMemberMetaData.InvocationExpression.ThrowWhenArgumentsAreNotMatched = false;
			return this;
		}

		public IReplaceMockConfiguration<TSut, TReturn> When(Func<bool> when)
		{
			_mock.SourceMemberMetaData.WhenFunc = when;
			return this;
		}

		public IReplaceMockConfiguration<TSut, TReturn> When(Func<IExecutor<TSut>, bool> when)
		{
			_mock.SourceMemberMetaData.WhenFunc = () => when(_executor);
			return this;
		}
	}
}
