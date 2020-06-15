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

        public SourceMemberInsertMockConfiguration CheckArguments()
        {
            _mock.CheckArguments = true;
            return this;
        }

        public SourceMemberInsertMockConfiguration ExpectedCalls(byte expectedCallsCount)
        {
            return ExpectedCalls(callsCount => callsCount == expectedCallsCount);
        }

        public SourceMemberInsertMockConfiguration ExpectedCalls(Func<byte, bool> expectedCallsCountFunc)
        {
            _mock.ExpectedCalls = expectedCallsCountFunc;
            return this;
        }
    }
}
