using AutoFake.Expression;
using System;
using System.Collections.Generic;

namespace AutoFake.Setup
{
    public class RemoveMockInstaller
    {
        private readonly ReplaceMock _mock;

        internal RemoveMockInstaller(ICollection<IMock> mocks, IInvocationExpression invocationExpression)
        {
            _mock = new ReplaceMock(invocationExpression);
            mocks.Add(_mock);
        }

        public RemoveMockInstaller CheckArguments()
        {
            _mock.CheckArguments = true;
            return this;
        }

        public RemoveMockInstaller ExpectedCalls(byte expectedCallsCount)
        {
            _mock.ExpectedCallsFunc = callsCount => callsCount == expectedCallsCount;
            return this;
        }

        public RemoveMockInstaller ExpectedCalls(Func<byte, bool> expectedCallsCountFunc)
        {
            _mock.ExpectedCallsFunc = expectedCallsCountFunc;
            return this;
        }
    }
}
