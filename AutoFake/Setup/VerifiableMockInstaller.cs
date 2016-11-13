using System.Collections.Generic;
using System.Reflection;

namespace AutoFake.Setup
{
    public class VerifiableMockInstaller : MockInstaller
    {
        internal VerifiableMockInstaller(SetupCollection setups, MethodInfo method, List<FakeArgument> setupArguments, bool isVoid)
            : base(method, setupArguments)
        {
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
