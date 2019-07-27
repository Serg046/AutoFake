using AutoFake.Expression;
using System;
using System.Collections.Generic;

namespace AutoFake.Setup
{
    public class RemoveMockInstaller
    {
        private readonly ReplaceMock.Parameters _parameters;

        internal RemoveMockInstaller(ICollection<IMock> mocks, IInvocationExpression invocationExpression)
        {
            _parameters = new ReplaceMock.Parameters();
            mocks.Add(new ReplaceMock(invocationExpression, _parameters));
        }

        public RemoveMockInstaller CheckArguments()
        {
            _parameters.CheckArguments = true;
            return this;
        }

        public RemoveMockInstaller ExpectedCalls(byte expectedCallsCount)
        {
            _parameters.ExpectedCallsFunc = callsCount => callsCount == expectedCallsCount;
            return this;
        }

        public RemoveMockInstaller ExpectedCalls(Func<byte, bool> expectedCallsCountFunc)
        {
            _parameters.ExpectedCallsFunc = expectedCallsCountFunc;
            return this;
        }
    }
}
