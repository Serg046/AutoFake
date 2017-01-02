using System;
using System.Collections.Generic;
using System.Reflection;
using AutoFake.Exceptions;
using AutoFake.Setup;
using Xunit;

namespace AutoFake.UnitTests.Setup.MockInstallerTests
{
    public class MockInstallerTests
    {
        [Fact]
        public void CheckArguments_IncorrectNumberOfArguments_Throws()
        {
            var method = GetMethod();

            List<FakeArgument> arguments = null;
            Assert.Throws<SetupException>(()
                => new ReplaceableMockInstaller<int>(new List<Mock>(), method, arguments).CheckArguments());
            Assert.Throws<SetupException>(()
                => new VerifiableMockInstaller(new List<Mock>(), method, arguments).CheckArguments());

            arguments = new List<FakeArgument>();
            var replaceableMockInstaller = new ReplaceableMockInstaller<int>(new List<Mock>(), method, arguments);
            var verifiableMockInstaller = new VerifiableMockInstaller(new List<Mock>(), method, arguments);

            Assert.Throws<SetupException>(() => replaceableMockInstaller.CheckArguments());
            Assert.Throws<SetupException>(() => verifiableMockInstaller.CheckArguments());

            arguments.Add(new FakeArgument(new EqualityArgumentChecker(1)));
            replaceableMockInstaller.CheckArguments();
            verifiableMockInstaller.CheckArguments();

            arguments.Add(new FakeArgument(new EqualityArgumentChecker(1)));
            Assert.Throws<SetupException>(() => replaceableMockInstaller.CheckArguments());
            Assert.Throws<SetupException>(() => verifiableMockInstaller.CheckArguments());
        }


        [Theory]
        [MemberData(nameof(GetInstallers))]
        public void ExpectedCallsCount_NonPositiveValue_Throws(dynamic installer)
        {
            Assert.Throws<SetupException>(() => installer.ExpectedCallsCount(-1));
            Assert.Throws<SetupException>(() => installer.ExpectedCallsCount(0));
            installer.ExpectedCallsCount(1);
        }

        [Fact]
        public void Returns_VoidMethod_Throws()
        {
            var arguments = new List<FakeArgument> {new FakeArgument(new EqualityArgumentChecker(1))};
            var installer = new ReplaceableMockInstaller<int>(new List<Mock>(), GetMethod(), arguments);

            Assert.Throws<SetupException>(() => installer.Returns(5));
        }

        [Fact]
        public void Callback_Null_Throws()
        {
            var arguments = new List<FakeArgument> {new FakeArgument(new EqualityArgumentChecker(1))};
            var installer = new ReplaceableMockInstaller<int>(new List<Mock>(), GetMethod(), arguments);

            Assert.Throws<SetupException>(() => installer.Callback(null));
        }

        public static IEnumerable<object[]> GetInstallers()
        {
            var arguments = new List<FakeArgument> {new FakeArgument(new EqualityArgumentChecker(1))};
            yield return new object[] {new ReplaceableMockInstaller<int>(new List<Mock>(), GetMethod(), arguments) };
            yield return new object[] {new VerifiableMockInstaller(new List<Mock>(), GetMethod(), arguments) };
        }

        private static MethodInfo GetMethod() => typeof(TestClass).GetMethod(nameof(TestClass.MockedMethod));

        private class TestClass
        {
            public void MockedMethod(int value)
            {
            }
        }
    }
}
