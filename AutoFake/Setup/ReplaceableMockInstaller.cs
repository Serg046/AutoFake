using System;
using System.Collections.Generic;
using System.Reflection;
using AutoFake.Exceptions;

namespace AutoFake.Setup
{
    public class ReplaceableMockInstaller<TReturn> : ReplaceableMockInstallerBase
    {
        private readonly MethodInfo _method;
        private readonly List<FakeArgument> _setupArguments;
        private readonly ReplaceableMock.Parameters _parameters;

        internal ReplaceableMockInstaller(ICollection<Mock> mocks, MethodInfo method, List<FakeArgument> setupArguments)
        {
            _method = method;
            _setupArguments = setupArguments;
            _parameters = new ReplaceableMock.Parameters();
            mocks.Add(new ReplaceableMock(method, setupArguments, _parameters));
        }

        public ReplaceableMockInstaller<TReturn> Returns(TReturn returnObject)
        {
            if (_method.ReturnType == typeof(void))
                throw new SetupException("Setup expression must be non-void method");
            _parameters.ReturnObject = returnObject;
            return this;
        }

        public ReplaceableMockInstaller<TReturn> CheckArguments()
        {
            ValidateSetupArguments(_method, _setupArguments);
            _parameters.NeedCheckArguments = true;
            return this;
        }

        public ReplaceableMockInstaller<TReturn> ExpectedCallsCount(int expectedCallsCount)
        {
            ValidateExpectedCallsCount(expectedCallsCount);
            _parameters.ExpectedCallsCountFunc = callsCount => callsCount == expectedCallsCount;
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
        private readonly MethodInfo _method;
        private readonly List<FakeArgument> _setupArguments;
        private readonly ReplaceableMock.Parameters _parameters;

        internal ReplaceableMockInstaller(ICollection<Mock> mocks, MethodInfo method, List<FakeArgument> setupArguments)
        {
            _method = method;
            _setupArguments = setupArguments;
            _parameters = new ReplaceableMock.Parameters();
            mocks.Add(new ReplaceableMock(method, setupArguments, _parameters));
        }

        public ReplaceableMockInstaller CheckArguments()
        {
            ValidateSetupArguments(_method, _setupArguments);
            _parameters.NeedCheckArguments = true;
            return this;
        }

        public ReplaceableMockInstaller ExpectedCallsCount(int expectedCallsCount)
        {
            ValidateExpectedCallsCount(expectedCallsCount);
            _parameters.ExpectedCallsCountFunc = callsCount => callsCount == expectedCallsCount;
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
