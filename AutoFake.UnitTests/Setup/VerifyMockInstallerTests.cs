using AutoFake.Expression;
using AutoFake.Setup;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace AutoFake.UnitTests.Setup
{
    public class VerifyMockInstallerTests
    {
        private readonly VerifyMock _mock;
        private readonly VerifyMockInstaller _verifyMockInstaller;

        public VerifyMockInstallerTests()
        {
            var mocks = new List<IMock>();
            _verifyMockInstaller = new VerifyMockInstaller(mocks, Moq.Mock.Of<IInvocationExpression>());
            _mock = (VerifyMock)mocks.Single();
        }

        [Fact]
        public void CheckArguments_ReturnsTrue()
        {
            Assert.False(_mock.CheckArguments);
            _verifyMockInstaller.CheckArguments();
            Assert.True(_mock.CheckArguments);
        }

        [Fact]
        public void ExpectedCalls_Byte_Success()
        {
            _verifyMockInstaller.ExpectedCalls(3);

            Assert.True(_mock.ExpectedCallsFunc(3));
            Assert.False(_mock.ExpectedCallsFunc(2));
        }

        [Fact]
        public void ExpectedCalls_Func_Success()
        {
            _verifyMockInstaller.ExpectedCalls(x => x > 2);

            Assert.True(_mock.ExpectedCallsFunc(3));
            Assert.False(_mock.ExpectedCallsFunc(2));
        }
    }
}
