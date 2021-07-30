using AutoFake.Setup.Mocks;
using System;

namespace AutoFake.Setup.Configurations
{
    public class VerifyMockConfiguration
    {
        private readonly VerifyMock _mock;

        internal VerifyMockConfiguration(VerifyMock mock)
        {
            _mock = mock;
        }

        public VerifyMockConfiguration ExpectedCalls(uint expectedCallsCount)
        {
            return ExpectedCalls(callsCount => callsCount == expectedCallsCount);
        }

        public VerifyMockConfiguration ExpectedCalls(Func<uint, bool> expectedCallsCountFunc)
        {
            _mock.ExpectedCalls = expectedCallsCountFunc;
            return this;
        }
    }
}
