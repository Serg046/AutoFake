using System;
using System.Collections.Generic;
using AutoFake.Expression;

namespace AutoFake.Setup
{
    public class ReplaceMockInstaller<TReturn>
    {
        private readonly ReplaceMock.Parameters _parameters;

        internal ReplaceMockInstaller(ICollection<IMock> mocks, IInvocationExpression invocationExpression)
        {
            _parameters = new ReplaceMock.Parameters();
            mocks.Add(new ReplaceMock(invocationExpression, _parameters));
        }

        public ReplaceMockInstaller<TReturn> Return(Func<TReturn> returnObject)
        {
            _parameters.ReturnObject = new MethodDescriptor(returnObject.Method.DeclaringType.FullName, returnObject.Method.Name);
            return this;
        }

        public ReplaceMockInstaller<TReturn> CheckArguments()
        {
            _parameters.CheckArguments = true;
            return this;
        }

        public ReplaceMockInstaller<TReturn> ExpectedCalls(byte expectedCallsCount)
        {
            _parameters.ExpectedCallsFunc = callsCount => callsCount == expectedCallsCount;
            return this;
        }

        public ReplaceMockInstaller<TReturn> ExpectedCalls(Func<byte, bool> expectedCallsCountFunc)
        {
            _parameters.ExpectedCallsFunc = expectedCallsCountFunc;
            return this;
        }

        public ReplaceMockInstaller<TReturn> Callback(Action callback)
        {
            _parameters.Callback = callback ?? throw new ArgumentNullException(nameof(callback));
            return this;
        }
    }

    public class ReplaceMockInstaller
    {
        private readonly ReplaceMock.Parameters _parameters;

        internal ReplaceMockInstaller(ICollection<IMock> mocks, IInvocationExpression invocationExpression)
        {
            _parameters = new ReplaceMock.Parameters();
            mocks.Add(new ReplaceMock(invocationExpression, _parameters));
        }

        public ReplaceMockInstaller CheckArguments()
        {
            _parameters.CheckArguments = true;
            return this;
        }

        public ReplaceMockInstaller ExpectedCalls(byte expectedCallsCount)
        {
            _parameters.ExpectedCallsFunc = callsCount => callsCount == expectedCallsCount;
            return this;
        }

        public ReplaceMockInstaller ExpectedCalls(Func<byte, bool> expectedCallsCountFunc)
        {
            _parameters.ExpectedCallsFunc = expectedCallsCountFunc;
            return this;
        }

        public ReplaceMockInstaller Callback(Action callback)
        {
            _parameters.Callback = callback ?? throw new ArgumentNullException(nameof(callback));
            return this;
        }
    }
}
