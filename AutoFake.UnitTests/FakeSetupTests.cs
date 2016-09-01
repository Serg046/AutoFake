using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using AutoFake.Exceptions;
using AutoFake.Setup;
using GuardExtensions;
using Xunit;

namespace AutoFake.UnitTests
{
    public class FakeSetupTests : ExpressionUnitTest
    {
        public delegate dynamic BuildFakeSetup(ICollection setups, MethodInfo method, object[] setupArgs);

        public static IEnumerable<object> FakeSetupBuilders
        {
            get
            {
                BuildFakeSetup buildFunc = (setups, method, args) => new ReplaceableMockInstaller<int>((dynamic)setups, method, args);
                yield return new object[] {buildFunc};
                buildFunc = (setups, method, args) => new ReplaceableMockInstaller((dynamic)setups, method, args);
                yield return new object[] { buildFunc };
            }
        }

        private MethodInfo GetMethodInfo()
        {
            var someMethodInfo = GetType().GetMethods()[0];
            if (someMethodInfo == null)
                throw new InvalidOperationException("MethodInfo is not found");
            return someMethodInfo;
        }

        [Theory]
        [MemberData(nameof(FakeSetupBuilders))]
        public void Ctor_IncorrectInputValues_Throws(BuildFakeSetup buildFakeSetup)
        {
            var someMethodInfo = GetMethodInfo();

            Assert.Throws<ContractFailedException>(() => buildFakeSetup(null, someMethodInfo, null));
            Assert.Throws<ContractFailedException>(() => buildFakeSetup(new Fake<SomeType>().Setups, null, null));
        }

        [Theory]
        [MemberData(nameof(FakeSetupBuilders))]
        public void Ctor_CorrectInputValues_NewSetupAdded(BuildFakeSetup buildFakeSetup)
        {
            var someMethodInfo = GetMethodInfo();
            var fake = new Fake<SomeType>();
            var args = new object[] {1};

            var setup = buildFakeSetup(fake.Setups, someMethodInfo, args);

            Assert.Equal(1, fake.Setups.Count);
            Assert.Equal(setup.FakeSetupPack, fake.Setups[0]);
            Assert.Equal(someMethodInfo, fake.Setups[0].Method);
            Assert.Equal(args, fake.Setups[0].SetupArguments);
            Assert.False(fake.Setups[0].NeedCheckCallsCount);
        }

        [Fact]
        public void VoidCtor_CorrectInputValues_IsVoidDefined()
        {
            var someMethodInfo = GetMethodInfo();
            var fake = new Fake<SomeType>();
            var args = new object[] { 1 };

            var setup = new ReplaceableMockInstaller(fake.Setups, someMethodInfo, args);

            Assert.Equal(1, fake.Setups.Count);
            Assert.Equal(setup.FakeSetupPack, fake.Setups[0]);
            Assert.Equal(someMethodInfo, fake.Setups[0].Method);
            Assert.Equal(args, fake.Setups[0].SetupArguments);
            Assert.False(fake.Setups[0].NeedCheckCallsCount);
            Assert.True(fake.Setups[0].IsVoid);
        }

        [Fact]
        public void Returns_SomeValue_SetupPackUpdated()
        {
            var someMethodInfo = GetMethodInfo();
            var fake = new Fake<SomeType>();

            var setup = new ReplaceableMockInstaller<object>(fake.Setups, someMethodInfo, null);

            setup.Returns(null);
            Assert.Equal(null, setup.FakeSetupPack.ReturnObject);

            setup.Returns(5);
            Assert.Equal(5, setup.FakeSetupPack.ReturnObject);
        }

        [Theory]
        [MemberData(nameof(FakeSetupBuilders))]
        public void Verifiable_NoSetupArguments_Throws(BuildFakeSetup buildFakeSetup)
        {
            var someMethodInfo = GetMethodInfo();
            var fake = new Fake<SomeType>();
            var setup = buildFakeSetup(fake.Setups, someMethodInfo, null);

            Assert.Throws<VerifiableException>(() => setup.CheckArguments());
        }

        [Theory]
        [MemberData(nameof(FakeSetupBuilders))]
        public void Verifiable_SetupArgument_Success(BuildFakeSetup buildFakeSetup)
        {
            var someMethodInfo = GetMethodInfo();
            var fake = new Fake<SomeType>();
            var args = new object[] {1};
            var setup = buildFakeSetup(fake.Setups, someMethodInfo, args);

            setup.CheckArguments();

            Assert.True(setup.FakeSetupPack.NeedCheckArguments);
        }

        [Theory]
        [MemberData(nameof(FakeSetupBuilders))]
        public void ExpectedCallsCount_IncorrectInputArg_Throws(BuildFakeSetup buildFakeSetup)
        {
            var someMethodInfo = GetMethodInfo();
            var fake = new Fake<SomeType>();
            var setup = buildFakeSetup(fake.Setups, someMethodInfo, null);

            Assert.Throws<ExpectedCallsException>(() => setup.ExpectedCallsCount(-1));
        }

        [Theory]
        [MemberData(nameof(FakeSetupBuilders))]
        public void ExpectedCallsCount_SomeValue_Success(BuildFakeSetup buildFakeSetup)
        {
            var someMethodInfo = GetMethodInfo();
            var fake = new Fake<SomeType>();
            var setup = buildFakeSetup(fake.Setups, someMethodInfo, null);

            setup.ExpectedCallsCount(1);

            Assert.True(setup.FakeSetupPack.ExpectedCallsCountFunc(1));
        }
    }
}
