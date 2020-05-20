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

        public VerifyMockConfiguration CheckArguments()
        {
            _mock.CheckArguments = true;
            return this;
        }

        //public VerifyMockInstaller ExpectedCalls(byte expectedCallsCount)
        //{
        //    return ExpectedCalls(callsCount => callsCount == expectedCallsCount);
        //}

        public VerifyMockConfiguration ExpectedCalls(Func<byte, bool> expectedCallsCountFunc)
        {
            _mock.ExpectedCalls = expectedCallsCountFunc.ToMethodDescriptor();
            return this;
        }
    }
}
