using System;
using System.Collections.Generic;
using AutoFake.Exceptions;
using AutoFake.Expression;

namespace AutoFake.Setup
{
    public class ReplaceableMockInstaller<TReturn> : ReplaceableMockInstallerBase
    {
        private readonly ISourceMember _sourceMember;
        private readonly IList<FakeArgument> _setupArguments;
        private readonly ReplaceableMock.Parameters _parameters;

        internal ReplaceableMockInstaller(ICollection<Mock> mocks, IInvocationExpression invocationExpression)
        {
            _sourceMember = invocationExpression.GetSourceMember();
            _setupArguments = invocationExpression.GetArguments();
            _parameters = new ReplaceableMock.Parameters();
            mocks.Add(new ReplaceableMock(_sourceMember, _setupArguments, _parameters));
        }

        public ReplaceableMockInstaller<TReturn> Returns(TReturn returnObject)
        {
            if (_sourceMember.ReturnType == typeof(void))
                throw new SetupException("Setup expression must be non-void method");
            _parameters.ReturnObject = returnObject;
            return this;
        }

        public ReplaceableMockInstaller<TReturn> CheckArguments()
        {
            ValidateSetupArguments(_sourceMember, _setupArguments);
            _parameters.NeedCheckArguments = true;
            return this;
        }

        public ReplaceableMockInstaller<TReturn> ExpectedCallsCount(int expectedCallsCount)
        {
            ValidateExpectedCallsCount(expectedCallsCount);
            _parameters.ExpectedCallsCountFunc = callsCount => callsCount == expectedCallsCount;
            return this;
        }

        public ReplaceableMockInstaller<TReturn> ExpectedCallsCount(Func<int,bool> expectedCallsCountFunc)
        {
            _parameters.ExpectedCallsCountFunc = expectedCallsCountFunc;
            return this;
        }

        public ReplaceableMockInstaller<TReturn> Callback(Action callback)
        {
            ValidateCallback(callback);
            _parameters.Callback = callback;
            return this;
        }
    }

    public class ReplaceableMockInstaller : ReplaceableMockInstallerBase
    {
        private readonly ISourceMember _sourceMember;
        private readonly IList<FakeArgument> _setupArguments;
        private readonly ReplaceableMock.Parameters _parameters;

        internal ReplaceableMockInstaller(ICollection<Mock> mocks, IInvocationExpression invocationExpression)
        {
            _sourceMember = invocationExpression.GetSourceMember();
            _setupArguments = invocationExpression.GetArguments();
            _parameters = new ReplaceableMock.Parameters();
            mocks.Add(new ReplaceableMock(_sourceMember, _setupArguments, _parameters));
        }

        public ReplaceableMockInstaller CheckArguments()
        {
            ValidateSetupArguments(_sourceMember, _setupArguments);
            _parameters.NeedCheckArguments = true;
            return this;
        }

        public ReplaceableMockInstaller ExpectedCallsCount(int expectedCallsCount)
        {
            ValidateExpectedCallsCount(expectedCallsCount);
            _parameters.ExpectedCallsCountFunc = callsCount => callsCount == expectedCallsCount;
            return this;
        }

        public ReplaceableMockInstaller ExpectedCallsCount(Func<int, bool> expectedCallsCountFunc)
        {
            _parameters.ExpectedCallsCountFunc = expectedCallsCountFunc;
            return this;
        }

        public ReplaceableMockInstaller Callback(Action callback)
        {
            ValidateCallback(callback);
            _parameters.Callback = callback;
            return this;
        }
    }
}
