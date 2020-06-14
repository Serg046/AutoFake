using AutoFake.Expression;
using AutoFake.Setup.Configurations;
using AutoFake.Setup.Mocks;
using Xunit;

namespace AutoFake.UnitTests.Setup
{
    public class VerifyMockInstallerTests
    {
        private readonly VerifyMock _mock;
        private readonly VerifyMockConfiguration _verifyMockInstaller;

        public VerifyMockInstallerTests()
        {
            _mock = new VerifyMock(new ProcessorFactory(null), Moq.Mock.Of<IInvocationExpression>());
            _verifyMockInstaller = new VerifyMockConfiguration(_mock, default);
        }

        [Fact]
        public void CheckArguments_ReturnsTrue()
        {
            Assert.False(_mock.CheckArguments);
            _verifyMockInstaller.CheckArguments();
            Assert.True(_mock.CheckArguments);
        }

        //[Fact]
        //public void ExpectedCalls_Byte_Success()
        //{
        //    _verifyMockInstaller.ExpectedCalls(3);

        //    Assert.True(_mock.ExpectedCallsFunc(3));
        //    Assert.False(_mock.ExpectedCallsFunc(2));
        //}

        [Fact]
        public void ExpectedCalls_Func_Success()
        {
            System.Func<byte, bool> func = x => x > 2;

            _verifyMockInstaller.ExpectedCalls(func);

            Assert.Equal(func.Method.Name, _mock.ExpectedCalls.Name);
            Assert.Equal(func.Method.DeclaringType.FullName, _mock.ExpectedCalls.DeclaringType);
        }
    }
}
