using AutoFake.Setup.Mocks;
using System;

namespace AutoFake.Setup.Configurations
{
    public class SourceMemberInsertMockConfiguration
    {
        private readonly SourceMemberInsertMock _mock;
        private readonly IProcessorFactory _processorFactory;

        internal SourceMemberInsertMockConfiguration(SourceMemberInsertMock mock, IProcessorFactory processorFactory)
        {
            _mock = mock;
            _processorFactory = processorFactory;
        }

        public SourceMemberInsertMockConfiguration CheckArguments()
        {
            _mock.CheckArguments = true;
            return this;
        }

        //public SourceMemberInsertMockInstaller ExpectedCalls(byte expectedCallsCount)
        //{
        //    return ExpectedCalls(callsCount => callsCount == expectedCallsCount);
        //}

        public SourceMemberInsertMockConfiguration ExpectedCalls(Func<byte, bool> expectedCallsCountFunc)
        {
            _mock.ExpectedCalls = expectedCallsCountFunc.ToClosureDescriptor(_processorFactory);
            return this;
        }
    }
}
