using System;
using System.Collections.Generic;
using System.Reflection;
using AutoFake.Exceptions;
using GuardExtensions;
using Xunit;

namespace AutoFake.UnitTests
{
    public class FakeSetupTests : ExpressionUnitTest
    {
        public delegate dynamic BuildFakeSetup(dynamic fake, MethodInfo method, object[] setupArgs);

        public static IEnumerable<object> FakeSetupBuilders
        {
            get
            {
                BuildFakeSetup buildFunc = (fake, method, args) => new FakeSetup<SomeType, int>(fake, method, args);
                yield return new object[] {buildFunc};
                buildFunc = (fake, method, args) => new FakeSetup<SomeType>(fake, method, args);
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
            Assert.Throws<ContractFailedException>(() => buildFakeSetup(new Fake<SomeType>(), null, null));
        }

        [Theory]
        [MemberData(nameof(FakeSetupBuilders))]
        public void Ctor_CorrectInputValues_NewSetupAdded(BuildFakeSetup buildFakeSetup)
        {
            var someMethodInfo = GetMethodInfo();
            var fake = new Fake<SomeType>();
            var args = new object[] {1};

            var setup = buildFakeSetup(fake, someMethodInfo, args);

            Assert.Equal(1, fake.Setups.Count);
            Assert.Equal(setup.FakeSetupPack, fake.Setups[0]);
            Assert.Equal(someMethodInfo, fake.Setups[0].Method);
            Assert.Equal(args, fake.Setups[0].SetupArguments);
            Assert.Equal(-1, fake.Setups[0].ExpectedCallsCount);
        }

        [Fact]
        public void VoidCtor_CorrectInputValues_IsVoidDefined()
        {
            var someMethodInfo = GetMethodInfo();
            var fake = new Fake<SomeType>();
            var args = new object[] { 1 };

            var setup = new FakeSetup<SomeType>(fake, someMethodInfo, args);

            Assert.Equal(1, fake.Setups.Count);
            Assert.Equal(setup.FakeSetupPack, fake.Setups[0]);
            Assert.Equal(someMethodInfo, fake.Setups[0].Method);
            Assert.Equal(args, fake.Setups[0].SetupArguments);
            Assert.Equal(-1, fake.Setups[0].ExpectedCallsCount);
            Assert.True(fake.Setups[0].IsVoid);
        }

        [Fact]
        public void Returns_SomeValue_SetupPackUpdated()
        {
            var someMethodInfo = GetMethodInfo();
            var fake = new Fake<SomeType>();

            var setup = new FakeSetup<SomeType, object>(fake, someMethodInfo, null);

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
            var setup = buildFakeSetup(fake, someMethodInfo, null);

            Assert.Throws<VerifiableException>(() => setup.Verifiable());
        }

        [Theory]
        [MemberData(nameof(FakeSetupBuilders))]
        public void Verifiable_SetupArgument_Success(BuildFakeSetup buildFakeSetup)
        {
            var someMethodInfo = GetMethodInfo();
            var fake = new Fake<SomeType>();
            var args = new object[] {1};
            var setup = buildFakeSetup(fake, someMethodInfo, args);

            setup.Verifiable();

            Assert.True(setup.FakeSetupPack.IsVerifiable);
        }

        [Theory]
        [MemberData(nameof(FakeSetupBuilders))]
        public void ExpectedCallsCount_IncorrectInputArg_Throws(BuildFakeSetup buildFakeSetup)
        {
            var someMethodInfo = GetMethodInfo();
            var fake = new Fake<SomeType>();
            var setup = buildFakeSetup(fake, someMethodInfo, null);

            Assert.Throws<ExpectedCallsException>(() => setup.ExpectedCallsCount(-1));
        }

        [Theory]
        [MemberData(nameof(FakeSetupBuilders))]
        public void ExpectedCallsCount_SomeValue_Success(BuildFakeSetup buildFakeSetup)
        {
            var someMethodInfo = GetMethodInfo();
            var fake = new Fake<SomeType>();
            var setup = buildFakeSetup(fake, someMethodInfo, null);

            setup.ExpectedCallsCount(1);

            Assert.Equal(1, setup.FakeSetupPack.ExpectedCallsCount);
        }
    }
}
