using System;
using AutoFake.Exceptions;
using AutoFake.Setup;
using GuardExtensions;
using Xunit;

namespace AutoFake.UnitTests.Setup
{
    public class VerifiableMockInstallerTests
    {
        public void SomeMethod() { }

        [Fact]
        public void Ctor_InvalidInput_Throws()
        {
            var method = GetType().GetMethod(nameof(SomeMethod));
            Assert.Throws<ContractFailedException>(() => new VerifiableMockInstaller(null, method, new object[0], true));
            Assert.Throws<ContractFailedException>(() => new VerifiableMockInstaller(new SetupCollection(), null, new object[0], true));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_ValidInpit_SetupInitialized(bool isVoid)
        {
            var method = GetType().GetMethod(nameof(SomeMethod));
            var arguments = new object[] {1};

            var installer = new VerifiableMockInstaller(new SetupCollection(), method, arguments, isVoid);

            Assert.Equal(method, installer.FakeSetupPack.Method);
            Assert.Equal(arguments, installer.FakeSetupPack.SetupArguments);
            Assert.True(installer.FakeSetupPack.IsVerification);
            Assert.Equal(isVoid, installer.FakeSetupPack.IsVoid);
        }

        [Fact]
        public void CheckArguments_InvalidInput_Throws()
        {
            var method = GetType().GetMethod(nameof(SomeMethod));
            var installer = new VerifiableMockInstaller(new SetupCollection(), method, new object[0], true);

            Assert.Throws<SetupException>(() => installer.CheckArguments());
        }

        [Fact]
        public void CheckArguments_ValidInput_Success()
        {
            var method = GetType().GetMethod(nameof(SomeMethod));
            var installer = new VerifiableMockInstaller(new SetupCollection(), method, new object[1], true);

            installer.CheckArguments();

            Assert.True(installer.FakeSetupPack.NeedCheckArguments);
        }

        [Fact]
        public void ExpectedCallsCount_InvalidInput_Throws()
        {
            var method = GetType().GetMethod(nameof(SomeMethod));
            var installer = new VerifiableMockInstaller(new SetupCollection(), method, new object[0], true);

            Assert.Throws<SetupException>(() => installer.ExpectedCallsCount(0));
        }

        [Fact]
        public void ExpectedCallsCount_ValidInput_Success()
        {
            var method = GetType().GetMethod(nameof(SomeMethod));
            var installer = new VerifiableMockInstaller(new SetupCollection(), method, new object[0], true);

            installer.ExpectedCallsCount(2);

            Assert.True(installer.FakeSetupPack.NeedCheckCallsCount);
            Func<int, bool> callsCountFunc = callsCount => callsCount == 2;
            Assert.Equal(callsCountFunc(2), installer.FakeSetupPack.ExpectedCallsCountFunc(2));
            Assert.Equal(callsCountFunc(22), installer.FakeSetupPack.ExpectedCallsCountFunc(22));
        }
    }
}
