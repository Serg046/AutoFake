using System;
using System.Collections.Generic;
using AutoFake.Expression;

namespace AutoFake.Setup
{
    public class VerifyMockInstaller
    {
        private readonly VerifyMock _mock;

        internal VerifyMockInstaller(ICollection<IMock> mocks, IInvocationExpression invocationExpression)
        {
            _mock = new VerifyMock(invocationExpression);
            mocks.Add(_mock);
        }

        public VerifyMockInstaller CheckArguments()
        {
            _mock.CheckArguments = true;
            return this;
        }

        public VerifyMockInstaller ExpectedCalls(byte expectedCallsCount)
        {
            _mock.ExpectedCallsFunc = callsCount => callsCount == expectedCallsCount;
            return this;
        }

        public VerifyMockInstaller ExpectedCalls(Func<byte, bool> expectedCallsCountFunc)
        {
            _mock.ExpectedCallsFunc = expectedCallsCountFunc;
            return this;
        }
    }
}
