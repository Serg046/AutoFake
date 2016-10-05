using System.Reflection;
using AutoFake.Exceptions;

namespace AutoFake.Setup
{
    public abstract class MockInstaller
    {
        internal readonly FakeSetupPack FakeSetupPack;

        internal MockInstaller(MethodInfo method, object[] setupArguments)
        {
            FakeSetupPack = new FakeSetupPack()
            {
                Method = method,
                SetupArguments = setupArguments
            };
        }

        protected void CheckArgumentsImpl()
        {
            if (FakeSetupPack.SetupArguments == null || FakeSetupPack.SetupArguments.Length == 0)
                throw new VerifiableException("Setup expression must contain a method with parameters");
            FakeSetupPack.NeedCheckArguments = true;
        }

        protected void ExpectedCallsCountImpl(int expectedCallsCount)
        {
            if (expectedCallsCount < 1)
                throw new ExpectedCallsException("ExpectedCallsCount must be greater than 0");
            FakeSetupPack.NeedCheckCallsCount = true;
            FakeSetupPack.ExpectedCallsCountFunc = callsCount => callsCount == expectedCallsCount;
        }
    }
}
