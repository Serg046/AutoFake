using System;
using System.Reflection;

namespace AutoFake
{
    public class FakeSetup<T, TReturn> : FakeSetup
    {
        private readonly Fake<T> _fake;
        private bool _isInstalled;

        internal FakeSetup(Fake<T> fake, MethodInfo method, object[] setupArguments)
            : base(method, setupArguments)
        {
            _fake = fake;
        }

        public Fake<T> Returns(TReturn returnObject)
        {
            if (_isInstalled)
                throw new InvalidOperationException("It is already installed");
            _isInstalled = true;
            FakeSetupPack.ReturnObject = returnObject;
            _fake.Setups.Add(FakeSetupPack);
            return _fake;
        }

        public FakeSetup<T, TReturn> Verifiable()
        {
            VerifiableImpl();
            return this;
        }

        public FakeSetup<T, TReturn> ExpectedCallsCount(int expectedCallsCount)
        {
            ExpectedCallsCountImpl(expectedCallsCount);
            return this;
        }
    }

    public class FakeSetup<T> : FakeSetup
    {
        private readonly Fake<T> _fake;
        private bool _isInstalled;

        internal FakeSetup(Fake<T> fake, MethodInfo method, object[] setupArguments)
            : base(method, setupArguments)
        {
            _fake = fake;
        }

        public Fake<T> Void()
        {
            if (_isInstalled)
                throw new InvalidOperationException("It is already installed");
            _isInstalled = true;
            FakeSetupPack.ReturnObject = null;
            FakeSetupPack.IsVoid = true;
            _fake.Setups.Add(FakeSetupPack);
            return _fake;
        }

        public FakeSetup<T> Verifiable()
        {
            VerifiableImpl();
            return this;
        }

        public FakeSetup<T> ExpectedCallsCount(int expectedCallsCount)
        {
            ExpectedCallsCountImpl(expectedCallsCount);
            return this;
        }
    }

    public class FakeSetup
    {
        internal readonly FakeSetupPack FakeSetupPack;

        internal FakeSetup(MethodInfo method, object[] setupArguments)
        {
            FakeSetupPack = new FakeSetupPack()
            {
                Method = method,
                SetupArguments = setupArguments,
                ExpectedCallsCount = -1
            };
        }

        protected void VerifiableImpl()
        {
            if (FakeSetupPack.IsVerifiable)
                throw new InvalidOperationException("Verifiable() is already called");
            if (FakeSetupPack.SetupArguments.Length == 0)
                throw new InvalidOperationException("Setup expression must contain a method with parameters");
            FakeSetupPack.IsVerifiable = true;
        }

        protected void ExpectedCallsCountImpl(int expectedCallsCount)
        {
            if (expectedCallsCount < 1)
                throw new InvalidOperationException("ExpectedCallsCount must be greater than 0");
            FakeSetupPack.ExpectedCallsCount = expectedCallsCount;
        }
    }
}
