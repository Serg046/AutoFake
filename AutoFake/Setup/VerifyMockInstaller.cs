using System;
using System.Collections.Generic;
using AutoFake.Expression;

namespace AutoFake.Setup
{
    public class VerifyMockInstaller
    {
        private readonly VerifyMock.Parameters _parameters = new VerifyMock.Parameters();

        internal VerifyMockInstaller(ICollection<IMock> mocks, IInvocationExpression invocationExpression)
        {
            mocks.Add(new VerifyMock(invocationExpression, _parameters));
        }

        public VerifyMockInstaller CheckArguments()
        {
            _parameters.CheckArguments = true;
            return this;
        }

        public VerifyMockInstaller ExpectedCalls(byte expectedCallsCount)
        {
            _parameters.ExpectedCallsFunc = callsCount => callsCount == expectedCallsCount;
            return this;
        }

        public VerifyMockInstaller ExpectedCalls(Func<byte, bool> expectedCallsCountFunc)
        {
            _parameters.ExpectedCallsFunc = expectedCallsCountFunc;
            return this;
        }
    }
}
