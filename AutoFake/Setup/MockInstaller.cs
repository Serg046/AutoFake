using System.Collections.Generic;
using System.Reflection;
using AutoFake.Exceptions;

namespace AutoFake.Setup
{
    public abstract class MockInstaller
    {
        internal void ValidateSetupArguments(MethodInfo method, IList<FakeArgument> setupArguments)
        {
            if (setupArguments?.Count != method.GetParameters().Length)
                throw new SetupException("Setup expression must contain a method with parameters");
        }

        internal void ValidateExpectedCallsCount(int expectedCallsCount)
        {
            if (expectedCallsCount < 1)
                throw new SetupException("ExpectedCallsCount must be greater than 0");
        }
    }
}
