using System.Reflection;
using GuardExtensions;

namespace AutoFake.Setup
{
    public class ReplaceableMockInstaller<TReturn> : MockInstaller
    {
        internal ReplaceableMockInstaller(SetupCollection setups, MethodInfo method, object[] setupArguments)
            : base(method, setupArguments)
        {
            Guard.AreNotNull(setups, method);
            setups.Add(FakeSetupPack);
        }

        public ReplaceableMockInstaller<TReturn> Returns(TReturn returnObject)
        {
            FakeSetupPack.ReturnObject = returnObject;
            return this;
        }

        public ReplaceableMockInstaller<TReturn> CheckArguments()
        {
            CheckArgumentsImpl();
            return this;
        }

        public ReplaceableMockInstaller<TReturn> ExpectedCallsCount(int expectedCallsCount)
        {
            ExpectedCallsCountImpl(expectedCallsCount);
            return this;
        }
    }

    public class ReplaceableMockInstaller : MockInstaller
    {
        internal ReplaceableMockInstaller(SetupCollection setups, MethodInfo method, object[] setupArguments)
            : base(method, setupArguments)
        {
            Guard.AreNotNull(setups, method);

            FakeSetupPack.ReturnObject = null;
            FakeSetupPack.IsVoid = true;
            setups.Add(FakeSetupPack);
        }

        public ReplaceableMockInstaller CheckArguments()
        {
            CheckArgumentsImpl();
            return this;
        }

        public ReplaceableMockInstaller ExpectedCallsCount(int expectedCallsCount)
        {
            ExpectedCallsCountImpl(expectedCallsCount);
            return this;
        }
    }
}
