using System.Collections.Generic;
using System.Reflection;

namespace AutoFake.Setup
{
    public class VerifiableMockInstaller : MockInstaller
    {
        private readonly MethodInfo _method;
        private readonly List<FakeArgument> _setupArguments;
        private readonly VerifiableMock.Parameters _parameters = new VerifiableMock.Parameters();

        internal VerifiableMockInstaller(ICollection<Mock> mocks, MethodInfo method, List<FakeArgument> setupArguments)
        {
            _method = method;
            _setupArguments = setupArguments;

            mocks.Add(new VerifiableMock(method, setupArguments, _parameters));
        }

        public VerifiableMockInstaller CheckArguments()
        {
            ValidateSetupArguments(_method, _setupArguments);
            _parameters.NeedCheckArguments = true;
            return this;
        }

        public VerifiableMockInstaller ExpectedCallsCount(int expectedCallsCount)
        {
            ValidateExpectedCallsCount(expectedCallsCount);
            _parameters.ExpectedCallsCountFunc = callsCount => callsCount == expectedCallsCount;
            return this;
        }
    }
}
