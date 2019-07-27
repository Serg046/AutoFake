using System;
using System.Collections.Generic;
using System.Linq;
using AutoFake.Expression;
using AutoFake.Setup;
using Moq;
using Xunit;
using Mock = AutoFake.Setup.Mock;

namespace AutoFake.UnitTests.Setup.MockInstallerTests
{
    public class MockInstallerTests
    {
        [Theory]
        [MemberData(nameof(GetInstallers))]
        internal void CheckArguments_ReturnsTrue(dynamic installer, Mock mock)
        {
            Assert.False(mock.CheckArguments);
            installer.CheckArguments();
            Assert.True(mock.CheckArguments);
        }

        [Theory]
        [MemberData(nameof(GetInstallers))]
        internal void ExpectedCallsCount_Func_Success(dynamic installer, Mock mock)
        {
            Func<byte, bool> expectedCallsFunc = x => x > 2;
            installer.ExpectedCalls(expectedCallsFunc);

            Assert.True(mock.ExpectedCalls(3));
            Assert.False(mock.ExpectedCalls(2));
        }

        public static IEnumerable<object[]> GetInstallers()
        {
            var arguments = new List<FakeArgument> { new FakeArgument(new EqualityArgumentChecker(1)) };
            var invocationExpression = new Mock<IInvocationExpression>();
            invocationExpression.Setup(e => e.GetSourceMember()).Returns(GetSourceMethod());
            var mocks = new List<IMock>();
            yield return new object[] { new RemoveMockInstaller(mocks, invocationExpression.Object), mocks.Single() };
            mocks = new List<IMock>();
            yield return new object[] { new ReplaceMockInstaller<int>(mocks, invocationExpression.Object), mocks.Single() };
            mocks = new List<IMock>();
            yield return new object[] { new VerifyMockInstaller(mocks, invocationExpression.Object), mocks.Single() };
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
