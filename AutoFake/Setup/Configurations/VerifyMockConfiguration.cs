using AutoFake.Setup.Mocks;
using System;

namespace AutoFake.Setup.Configurations
{
    public class VerifyMockConfiguration
    {
        private readonly VerifyMock _mock;
        private readonly IProcessorFactory _processorFactory;

        internal VerifyMockConfiguration(VerifyMock mock, IProcessorFactory processorFactory)
        {
            _mock = mock;
            _processorFactory = processorFactory;
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
            _mock.ExpectedCalls = expectedCallsCountFunc.ToClosureDescriptor(_processorFactory);
            return this;
        }
    }
}
