using AutoFake.Setup.Mocks;
using System;

namespace AutoFake.Setup.Configurations
{
    public class RemoveMockConfiguration
    {
        private readonly ReplaceMock _mock;

        internal RemoveMockConfiguration(ReplaceMock mock)
        {
            _mock = mock;
        }

        public RemoveMockConfiguration ExpectedCalls(uint expectedCallsCount)
        {
            return ExpectedCalls(callsCount => callsCount == expectedCallsCount);
        }

        public RemoveMockConfiguration ExpectedCalls(Func<uint, bool> expectedCallsCountFunc)
        {
            _mock.ExpectedCalls = expectedCallsCountFunc;
            return this;
        }

        public RemoveMockConfiguration WhenArgumentsAreMatched()
        {
	        _mock.InvocationExpression.ThrowWhenArgumentsAreNotMatched = false;
	        return this;
        }
    }
}
