using AutoFake.Setup.Mocks;
using System;

namespace AutoFake.Setup.Configurations
{
    public class SourceMemberInsertMockConfiguration
    {
        private readonly SourceMemberInsertMock _mock;

        internal SourceMemberInsertMockConfiguration(SourceMemberInsertMock mock)
        {
            _mock = mock;
        }

        public SourceMemberInsertMockConfiguration ExpectedCalls(uint expectedCallsCount)
        {
            return ExpectedCalls(callsCount => callsCount == expectedCallsCount);
        }

        public SourceMemberInsertMockConfiguration ExpectedCalls(IExecutionContext.CallsCheckerFunc expectedCallsCountFunc)
        {
            _mock.ExpectedCalls = expectedCallsCountFunc;
            return this;
        }

        public SourceMemberInsertMockConfiguration WhenArgumentsAreMatched()
        {
	        _mock.InvocationExpression.ThrowWhenArgumentsAreNotMatched = false;
	        return this;
        }
    }
}
