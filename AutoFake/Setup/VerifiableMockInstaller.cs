using System.Reflection;
using GuardExtensions;

namespace AutoFake.Setup
{
    public class VerifiableMockInstaller : MockInstaller
    {
        internal VerifiableMockInstaller(SetupCollection setups, MethodInfo method, object[] setupArguments, bool isVoid)
            : base(method, setupArguments)
        {
            Guard.AreNotNull(setups, method);

            FakeSetupPack.IsVerification = true;
            FakeSetupPack.IsVoid = isVoid;
            setups.Add(FakeSetupPack);
        }

        public VerifiableMockInstaller CheckArguments()
        {
            CheckArgumentsImpl();
            return this;
        }

        public VerifiableMockInstaller ExpectedCallsCount(int expectedCallsCount)
        {
            ExpectedCallsCountImpl(expectedCallsCount);
            return this;
        }
    }
}
