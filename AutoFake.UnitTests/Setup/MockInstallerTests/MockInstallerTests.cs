using System.Collections.Generic;
using System.Reflection;
using AutoFake.Exceptions;
using AutoFake.Expression;
using AutoFake.Setup;
using Moq;
using Xunit;
using Mock = AutoFake.Setup.Mock;

namespace AutoFake.UnitTests.Setup.MockInstallerTests
{
    public class MockInstallerTests
    {
        [Fact]
        public void CheckArguments_IncorrectNumberOfArguments_Throws()
        {
            var method = GetSourceMethod();
            var invocationExpression = new Mock<IInvocationExpression>();
            invocationExpression.Setup(e => e.GetSourceMember()).Returns(method);
            invocationExpression.Setup(e => e.GetArguments()).Returns((IList<FakeArgument>)null);

            Assert.Throws<SetupException>(()
                => new ReplaceableMockInstaller<int>(new List<Mock>(), invocationExpression.Object).CheckArguments());
            Assert.Throws<SetupException>(()
                => new ReplaceableMockInstaller(new List<Mock>(), invocationExpression.Object).CheckArguments());
            Assert.Throws<SetupException>(()
                => new VerifiableMockInstaller(new List<Mock>(), invocationExpression.Object).CheckArguments());

            var arguments = new List<FakeArgument>();
            invocationExpression.Setup(e => e.GetArguments()).Returns(arguments);
            var replaceableMockInstaller = new ReplaceableMockInstaller(new List<Mock>(), invocationExpression.Object);
            var replaceableGenericMockInstaller = new ReplaceableMockInstaller<int>(new List<Mock>(), invocationExpression.Object);
            var verifiableMockInstaller = new VerifiableMockInstaller(new List<Mock>(), invocationExpression.Object);

            Assert.Throws<SetupException>(() => replaceableMockInstaller.CheckArguments());
            Assert.Throws<SetupException>(() => replaceableGenericMockInstaller.CheckArguments());
            Assert.Throws<SetupException>(() => verifiableMockInstaller.CheckArguments());

            arguments.Add(new FakeArgument(new EqualityArgumentChecker(1)));
            replaceableMockInstaller.CheckArguments();
            replaceableGenericMockInstaller.CheckArguments();
            verifiableMockInstaller.CheckArguments();

            arguments.Add(new FakeArgument(new EqualityArgumentChecker(1)));
            Assert.Throws<SetupException>(() => replaceableMockInstaller.CheckArguments());
            Assert.Throws<SetupException>(() => replaceableGenericMockInstaller.CheckArguments());
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
            var invocationExpression = new Mock<IInvocationExpression>();
            invocationExpression.Setup(e => e.GetSourceMember()).Returns(GetSourceMethod());
            invocationExpression.Setup(e => e.GetArguments()).Returns(arguments);
            var installer = new ReplaceableMockInstaller<int>(new List<Mock>(), invocationExpression.Object);

            Assert.Throws<SetupException>(() => installer.Returns(5));
        }

        [Fact]
        public void Callback_Null_Throws()
        {
            var arguments = new List<FakeArgument> {new FakeArgument(new EqualityArgumentChecker(1))};
            var invocationExpression = new Mock<IInvocationExpression>();
            invocationExpression.Setup(e => e.GetSourceMember()).Returns(GetSourceMethod());
            invocationExpression.Setup(e => e.GetArguments()).Returns(arguments);
            var genericInstaller = new ReplaceableMockInstaller<int>(new List<Mock>(), invocationExpression.Object);
            var installer = new ReplaceableMockInstaller<int>(new List<Mock>(), invocationExpression.Object);

            Assert.Throws<SetupException>(() => genericInstaller.Callback(null));
            Assert.Throws<SetupException>(() => installer.Callback(null));
        }

        public static IEnumerable<object[]> GetInstallers()
        {
            var arguments = new List<FakeArgument> {new FakeArgument(new EqualityArgumentChecker(1))};
            var invocationExpression = new Mock<IInvocationExpression>();
            invocationExpression.Setup(e => e.GetSourceMember()).Returns(GetSourceMethod());
            invocationExpression.Setup(e => e.GetArguments()).Returns(arguments);
            yield return new object[] {new ReplaceableMockInstaller(new List<Mock>(), invocationExpression.Object) };
            yield return new object[] {new ReplaceableMockInstaller<int>(new List<Mock>(), invocationExpression.Object) };
            yield return new object[] {new VerifiableMockInstaller(new List<Mock>(), invocationExpression.Object) };
        }

        private static ISourceMember GetSourceMethod()
            => new SourceMethod(typeof(TestClass).GetMethod(nameof(TestClass.MockedMethod)));

        private class TestClass
        {
            public void MockedMethod(int value)
            {
            }
        }
    }
}
