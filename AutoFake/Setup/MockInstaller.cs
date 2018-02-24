using System;
using System.Collections.Generic;
using System.Linq;
using AutoFake.Exceptions;

namespace AutoFake.Setup
{
    public abstract class MockInstaller
    {
        internal void ValidateSetupArguments(ISourceMember sourceMember, IList<FakeArgument> setupArguments)
        {
            if (setupArguments?.Count != sourceMember.GetParameters().Length)
                throw new SetupException("Setup expression must contain a method with parameters");
        }

        internal void ValidateExpectedCallsCount(int expectedCallsCount)
        {
            if (expectedCallsCount < 1)
                throw new SetupException("ExpectedCallsCount must be greater than 0");
        }
    }

    public abstract class ReplaceableMockInstallerBase : MockInstaller
    {
        internal void ValidateSourceMember(ICollection<Mock> mocks, ISourceMember soureMember)
        {
            if (mocks.Any(mock => mock.SourceMember.Equals(soureMember)))
            {
                throw new SetupException($"Mock for the following member is already installed: '{soureMember}'.");
            }
        }

        internal void ValidateCallback(Action callback)
        {
            if (callback == null)
                throw new SetupException("Callback must be not null");
        }
    }
}
