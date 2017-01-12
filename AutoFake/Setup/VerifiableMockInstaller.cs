using System.Collections.Generic;
using AutoFake.Expression;

namespace AutoFake.Setup
{
    public class VerifiableMockInstaller : MockInstaller
    {
        private readonly ISourceMember _sourceMember;
        private readonly IList<FakeArgument> _setupArguments;
        private readonly VerifiableMock.Parameters _parameters = new VerifiableMock.Parameters();

        internal VerifiableMockInstaller(ICollection<Mock> mocks, IInvocationExpression invocationExpression)
        {
            _sourceMember = invocationExpression.GetSourceMember();
            _setupArguments = invocationExpression.GetArguments();

            mocks.Add(new VerifiableMock(_sourceMember, _setupArguments, _parameters));
        }

        public VerifiableMockInstaller CheckArguments()
        {
            ValidateSetupArguments(_sourceMember, _setupArguments);
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
