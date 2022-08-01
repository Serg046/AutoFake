using AutoFake.Abstractions;
using AutoFake.Abstractions.Setup.Configurations;
using AutoFake.Setup.Mocks;
using System;

namespace AutoFake.Setup.Configurations
{
	internal class RemoveMockConfiguration<TSut> : IRemoveMockConfiguration<TSut>
    {
        private readonly ReplaceMock _mock;

        internal RemoveMockConfiguration(ReplaceMock mock)
        {
            _mock = mock;
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

        public IRemoveMockConfiguration<TSut> When(IExecutionContext.WhenInstanceFunc when)
        {
            _mock.SourceMemberMetaData.WhenFunc = when;
            return this;
        }
    }
}
