﻿using System;
using System.Collections.Generic;
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
            Assert.Throws<ContractFailedException>(() => new VerifiableMockInstaller(null, method, new List<FakeArgument>(), true));
            Assert.Throws<ContractFailedException>(() => new VerifiableMockInstaller(new SetupCollection(), null, new List<FakeArgument>(), true));
        }

        private static FakeArgument GetFakeArgument(dynamic value)
            => new FakeArgument(new EqualityArgumentChecker(value));

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_ValidInpit_SetupInitialized(bool isVoid)
        {
            var method = GetType().GetMethod(nameof(SomeMethod));
            var arguments = new List<FakeArgument> {GetFakeArgument(1)};

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
            var installer = new VerifiableMockInstaller(new SetupCollection(), method, new List<FakeArgument>(), true);

            Assert.Throws<SetupException>(() => installer.CheckArguments());
        }

        [Fact]
        public void CheckArguments_ValidInput_Success()
        {
            var method = GetType().GetMethod(nameof(SomeMethod));
            var installer = new VerifiableMockInstaller(new SetupCollection(), method,
                new List<FakeArgument> {GetFakeArgument(1)}, true);

            installer.CheckArguments();

            Assert.True(installer.FakeSetupPack.NeedCheckArguments);
        }

        [Fact]
        public void ExpectedCallsCount_InvalidInput_Throws()
        {
            var method = GetType().GetMethod(nameof(SomeMethod));
            var installer = new VerifiableMockInstaller(new SetupCollection(), method, new List<FakeArgument>(), true);

            Assert.Throws<SetupException>(() => installer.ExpectedCallsCount(0));
        }

        [Fact]
        public void ExpectedCallsCount_ValidInput_Success()
        {
            var method = GetType().GetMethod(nameof(SomeMethod));
            var installer = new VerifiableMockInstaller(new SetupCollection(), method, new List<FakeArgument>(), true);

            installer.ExpectedCallsCount(2);

            Assert.True(installer.FakeSetupPack.NeedCheckCallsCount);
            Func<int, bool> callsCountFunc = callsCount => callsCount == 2;
            Assert.Equal(callsCountFunc(2), installer.FakeSetupPack.ExpectedCallsCountFunc(2));
            Assert.Equal(callsCountFunc(22), installer.FakeSetupPack.ExpectedCallsCountFunc(22));
        }
    }
}