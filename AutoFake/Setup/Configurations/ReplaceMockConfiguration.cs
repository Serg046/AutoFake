using AutoFake.Setup.Mocks;
using System;

namespace AutoFake.Setup.Configurations
{
    public class ReplaceMockConfiguration<TReturn>
    {
        private readonly ReplaceMock _mock;

        internal ReplaceMockConfiguration(ReplaceMock mock)
        {
            _mock = mock;
        }

        public ReplaceMockConfiguration<TReturn> Return(TReturn returnObject)
        {
            _mock.ReturnObject = returnObject;
            return this;
        }

        public ReplaceMockConfiguration<TReturn> ExpectedCalls(uint expectedCallsCount)
        {
            return ExpectedCalls(callsCount => callsCount == expectedCallsCount);
        }

        public ReplaceMockConfiguration<TReturn> ExpectedCalls(Func<uint, bool> expectedCallsCountFunc)
        {
            _mock.ExpectedCalls = expectedCallsCountFunc;
            return this;
        }
    }
}
