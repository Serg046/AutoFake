using AutoFake.Setup.Mocks;
using System;

namespace AutoFake.Setup.Configurations
{
    public class RemoveMockConfiguration
    {
        private readonly ReplaceMock _mock;
        private readonly IProcessorFactory _processorFactory;

        internal RemoveMockConfiguration(ReplaceMock mock, IProcessorFactory processorFactory)
        {
            _mock = mock;
            _processorFactory = processorFactory;
        }

        public RemoveMockConfiguration CheckArguments()
        {
            _mock.CheckArguments = true;
            return this;
        }

        //public RemoveMockInstaller ExpectedCalls(byte expectedCallsCount)
        //{
        //    return ExpectedCalls(callsCount => callsCount == expectedCallsCount);
        //}

        public RemoveMockConfiguration ExpectedCalls(Func<byte, bool> expectedCallsCountFunc)
        {
            _mock.ExpectedCalls = expectedCallsCountFunc.ToClosureDescriptor(_processorFactory);
            return this;
        }
    }
}
