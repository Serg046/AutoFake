using System;
using System.Collections.Generic;
using AutoFake.Expression;

namespace AutoFake.Setup
{
    public class ReplaceMockInstaller<TReturn>
    {
        private readonly ReplaceMock _mock;

        internal ReplaceMockInstaller(ICollection<IMock> mocks, IInvocationExpression invocationExpression)
        {
            _mock = new ReplaceMock(invocationExpression);
            mocks.Add(_mock);
        }

        public ReplaceMockInstaller<TReturn> Return(Func<TReturn> returnObject)
        {
            _mock.ReturnObject = new MethodDescriptor(returnObject.Method.DeclaringType.FullName, returnObject.Method.Name);
            return this;
        }

        public ReplaceMockInstaller<TReturn> CheckArguments()
        {
            _mock.CheckArguments = true;
            return this;
        }

        public ReplaceMockInstaller<TReturn> ExpectedCalls(byte expectedCallsCount)
        {
            _mock.ExpectedCallsFunc = callsCount => callsCount == expectedCallsCount;
            return this;
        }

        public ReplaceMockInstaller<TReturn> ExpectedCalls(Func<byte, bool> expectedCallsCountFunc)
        {
            _mock.ExpectedCallsFunc = expectedCallsCountFunc;
            return this;
        }
    }
}
