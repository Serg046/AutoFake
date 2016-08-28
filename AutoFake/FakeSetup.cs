using System;
using System.Reflection;
using AutoFake.Exceptions;
using GuardExtensions;

namespace AutoFake
{
    public class FakeSetup<T, TReturn> : FakeSetup
    {
        internal FakeSetup(Fake<T> fake, MethodInfo method, object[] setupArguments)
            : base(method, setupArguments)
        {
            Guard.AreNotNull(fake, method);
            fake.Setups.Add(FakeSetupPack);
        }

        public void Returns(TReturn returnObject)
        {
            FakeSetupPack.ReturnObject = returnObject;
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
        internal FakeSetup(Fake<T> fake, MethodInfo method, object[] setupArguments)
            : base(method, setupArguments)
        {
            Guard.AreNotNull(fake, method);

            FakeSetupPack.ReturnObject = null;
            FakeSetupPack.IsVoid = true;
            fake.Setups.Add(FakeSetupPack);
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

    public abstract class FakeSetup
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
            if (FakeSetupPack.SetupArguments == null || FakeSetupPack.SetupArguments.Length == 0)
                throw new VerifiableException("Setup expression must contain a method with parameters");
            FakeSetupPack.IsVerifiable = true;
        }

        protected void ExpectedCallsCountImpl(int expectedCallsCount)
        {
            if (expectedCallsCount < 1)
                throw new ExpectedCallsException("ExpectedCallsCount must be greater than 0");
            FakeSetupPack.ExpectedCallsCount = expectedCallsCount;
        }
    }
}
