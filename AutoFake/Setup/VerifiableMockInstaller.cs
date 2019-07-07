using System;
using System.Collections.Generic;
using AutoFake.Expression;

namespace AutoFake.Setup
{
    public class VerifiableMockInstaller
    {
        private readonly VerifiableMock.Parameters _parameters = new VerifiableMock.Parameters();

        internal VerifiableMockInstaller(ICollection<IMock> mocks, IInvocationExpression invocationExpression)
        {
            mocks.Add(new VerifiableMock(invocationExpression, _parameters));
        }

        public VerifiableMockInstaller CheckArguments()
        {
            _parameters.NeedCheckArguments = true;
            return this;
        }

        public VerifiableMockInstaller ExpectedCallsCount(byte expectedCallsCount)
        {
            _parameters.ExpectedCallsCountFunc = callsCount => callsCount == expectedCallsCount;
            return this;
        }

        public VerifiableMockInstaller ExpectedCallsCount(Func<byte, bool> expectedCallsCountFunc)
        {
            _parameters.ExpectedCallsCountFunc = expectedCallsCountFunc;
            return this;
        }
    }
}
