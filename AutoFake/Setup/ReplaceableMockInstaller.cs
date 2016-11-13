﻿using System.Collections.Generic;
using System.Reflection;
using GuardExtensions;

namespace AutoFake.Setup
{
    public class ReplaceableMockInstaller<TReturn> : MockInstaller
    {
        internal ReplaceableMockInstaller(SetupCollection setups, MethodInfo method, List<FakeArgument> setupArguments)
            : base(method, setupArguments)
        {
            setups.Add(FakeSetupPack);
        }

        public ReplaceableMockInstaller<TReturn> Returns(TReturn returnObject)
        {
            FakeSetupPack.IsReturnObjectSet = true;
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
        internal ReplaceableMockInstaller(SetupCollection setups, MethodInfo method, List<FakeArgument> setupArguments)
            : base(method, setupArguments)
        {
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
