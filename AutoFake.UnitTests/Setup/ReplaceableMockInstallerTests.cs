﻿using System;
using System.Collections.Generic;
using AutoFake.Exceptions;
using AutoFake.Setup;
using Xunit;

namespace AutoFake.UnitTests.Setup
{
    public class ReplaceableMockInstallerTests
    {
        public int SomeRetValueMethod() => 1;
        public void SomeVoidMethod() { }

        private static FakeArgument GetFakeArgument(dynamic value)
            => new FakeArgument(new EqualityArgumentChecker(value));

        [Fact]
        public void Ctor_RetValueMethod_SetupInitialized()
        {
            var method = GetType().GetMethod(nameof(SomeRetValueMethod));
            var arguments = new List<FakeArgument> {GetFakeArgument(1)};

            var installer = new ReplaceableMockInstaller<int>(new SetupCollection(), method, arguments);

            Assert.Equal(method, installer.FakeSetupPack.Method);
            Assert.Equal(arguments, installer.FakeSetupPack.SetupArguments);
        }

        [Fact]
        public void Ctor_VoidMethod_SetupInitialized()
        {
            var method = GetType().GetMethod(nameof(SomeVoidMethod));
            var arguments = new List<FakeArgument> { GetFakeArgument(1) };

            var installer = new ReplaceableMockInstaller(new SetupCollection(), method, arguments);

            Assert.Equal(method, installer.FakeSetupPack.Method);
            Assert.Equal(arguments, installer.FakeSetupPack.SetupArguments);
            Assert.Null(installer.FakeSetupPack.ReturnObject);
            Assert.True(installer.FakeSetupPack.IsVoid);
        }

        [Fact]
        public void Returns_ValidInput_ReturnObjectIsSet()
        {
            var method = GetType().GetMethod(nameof(SomeRetValueMethod));
            var installer = new ReplaceableMockInstaller<int>(new SetupCollection(), method, new List<FakeArgument>());

            installer.Returns(7);

            Assert.Equal(7, installer.FakeSetupPack.ReturnObject);
            Assert.True(installer.FakeSetupPack.IsReturnObjectSet);
        }

        public static IEnumerable<object[]> GetMockInstallerTestData(object argument)
        {
            var arguments = argument == null
                ? new List<FakeArgument>()
                : new List<FakeArgument> { GetFakeArgument(argument)};

            var method = typeof(ReplaceableMockInstallerTests).GetMethod(nameof(SomeRetValueMethod));
            yield return
                new object[] { new ReplaceableMockInstaller<int>(new SetupCollection(), method, arguments) };

            method = typeof(ReplaceableMockInstallerTests).GetMethod(nameof(SomeVoidMethod));
            yield return
                new object[] { new ReplaceableMockInstaller(new SetupCollection(), method, arguments) };
        }

        [Theory]
        [MemberData(nameof(GetMockInstallerTestData), null)]
        public void CheckArguments_InvalidInput_Throws(MockInstaller installer)
        {
            Assert.Throws<SetupException>(() => ((dynamic)installer).CheckArguments());
        }

        [Theory]
        [MemberData(nameof(GetMockInstallerTestData), 1)]
        public void CheckArguments_ValidInput_Success(MockInstaller installer)
        {
            ((dynamic)installer).CheckArguments();

            Assert.True(installer.FakeSetupPack.NeedCheckArguments);
        }

        [Theory]
        [MemberData(nameof(GetMockInstallerTestData), 1)]
        public void ExpectedCallsCount_InvalidInput_Throws(MockInstaller installer)
        {
            Assert.Throws<SetupException>(() => ((dynamic)installer).ExpectedCallsCount(0));
        }

        [Theory]
        [MemberData(nameof(GetMockInstallerTestData), 1)]
        public void ExpectedCallsCount_ValidInput_Success(MockInstaller installer)
        {
            ((dynamic)installer).ExpectedCallsCount(2);

            Assert.True(installer.FakeSetupPack.NeedCheckCallsCount);
            Func<int, bool> callsCountFunc = callsCount => callsCount == 2;
            Assert.Equal(callsCountFunc(2), installer.FakeSetupPack.ExpectedCallsCountFunc(2));
            Assert.Equal(callsCountFunc(22), installer.FakeSetupPack.ExpectedCallsCountFunc(22));
        }

        [Theory]
        [MemberData(nameof(GetMockInstallerTestData), null)]
        public void Callback_InvalidInput_Throws(MockInstaller installer)
        {
            Assert.Throws<SetupException>(() => ((dynamic)installer).Callback(null));
        }
    }
}
