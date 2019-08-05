using System;

namespace AutoFake.Setup
{
    public class SourceMemberInsertMockInstaller
    {
        private readonly SourceMemberInsertMock _mock;

        internal SourceMemberInsertMockInstaller(SourceMemberInsertMock mock)
        {
            _mock = mock;
        }

        public SourceMemberInsertMockInstaller CheckArguments()
        {
            _mock.CheckArguments = true;
            return this;
        }

        public SourceMemberInsertMockInstaller ExpectedCalls(byte expectedCallsCount)
        {
            _mock.ExpectedCallsFunc = callsCount => callsCount == expectedCallsCount;
            return this;
        }

        public SourceMemberInsertMockInstaller ExpectedCalls(Func<byte, bool> expectedCallsCountFunc)
        {
            _mock.ExpectedCallsFunc = expectedCallsCountFunc;
            return this;
        }
    }
}
