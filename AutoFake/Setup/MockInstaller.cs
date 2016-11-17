using System;
using System.Collections.Generic;
using System.Reflection;
using AutoFake.Exceptions;

namespace AutoFake.Setup
{
    public abstract class MockInstaller
    {
        internal readonly FakeSetupPack FakeSetupPack;

        internal MockInstaller(MethodInfo method, List<FakeArgument> setupArguments)
        {
            FakeSetupPack = new FakeSetupPack()
            {
                Method = method,
                SetupArguments = setupArguments
            };
        }

        protected void CheckArgumentsImpl()
        {
            if (FakeSetupPack.SetupArguments == null || FakeSetupPack.SetupArguments.Count == 0)
                throw new SetupException("Setup expression must contain a method with parameters");
            FakeSetupPack.NeedCheckArguments = true;
        }

        protected void ExpectedCallsCountImpl(int expectedCallsCount)
        {
            if (expectedCallsCount < 1)
                throw new SetupException("ExpectedCallsCount must be greater than 0");
            FakeSetupPack.NeedCheckCallsCount = true;
            FakeSetupPack.ExpectedCallsCountFunc = callsCount => callsCount == expectedCallsCount;
        }

        protected void CallbackImpl(Action callback)
        {
            if (callback == null)
                throw new SetupException("Callback must be not null");
            FakeSetupPack.Callback = callback;
        }
    }
}
