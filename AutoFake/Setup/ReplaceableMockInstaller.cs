using System;
using System.Collections.Generic;
using AutoFake.Expression;

namespace AutoFake.Setup
{
    public class ReplaceableMockInstaller<TReturn>
    {
        private readonly ReplaceableMock.Parameters _parameters;

        internal ReplaceableMockInstaller(ICollection<Mock> mocks, IInvocationExpression invocationExpression)
        {
            _parameters = new ReplaceableMock.Parameters();
            mocks.Add(new ReplaceableMock(invocationExpression, _parameters));
        }

        public ReplaceableMockInstaller<TReturn> Returns(Func<TReturn> returnObject)
        {
            _parameters.ReturnObject = new MethodDescriptor(returnObject.Method.DeclaringType.FullName, returnObject.Method.Name);
            return this;
        }

        public ReplaceableMockInstaller<TReturn> CheckArguments()
        {
            _parameters.NeedCheckArguments = true;
            return this;
        }

        public ReplaceableMockInstaller<TReturn> ExpectedCallsCount(byte expectedCallsCount)
        {
            _parameters.ExpectedCallsCountFunc = callsCount => callsCount == expectedCallsCount;
            return this;
        }

        public ReplaceableMockInstaller<TReturn> ExpectedCallsCount(Func<byte, bool> expectedCallsCountFunc)
        {
            _parameters.ExpectedCallsCountFunc = expectedCallsCountFunc;
            return this;
        }

        public ReplaceableMockInstaller<TReturn> Callback(Action callback)
        {
            _parameters.Callback = callback ?? throw new ArgumentNullException(nameof(callback));
            return this;
        }
    }

    public class ReplaceableMockInstaller
    {
        private readonly ReplaceableMock.Parameters _parameters;

        internal ReplaceableMockInstaller(ICollection<Mock> mocks, IInvocationExpression invocationExpression)
        {
            _parameters = new ReplaceableMock.Parameters();
            mocks.Add(new ReplaceableMock(invocationExpression, _parameters));
        }

        public ReplaceableMockInstaller CheckArguments()
        {
            _parameters.NeedCheckArguments = true;
            return this;
        }

        public ReplaceableMockInstaller ExpectedCallsCount(byte expectedCallsCount)
        {
            _parameters.ExpectedCallsCountFunc = callsCount => callsCount == expectedCallsCount;
            return this;
        }

        public ReplaceableMockInstaller ExpectedCallsCount(Func<byte, bool> expectedCallsCountFunc)
        {
            _parameters.ExpectedCallsCountFunc = expectedCallsCountFunc;
            return this;
        }

        public ReplaceableMockInstaller Callback(Action callback)
        {
            _parameters.Callback = callback ?? throw new ArgumentNullException(nameof(callback));
            return this;
        }
    }
}
