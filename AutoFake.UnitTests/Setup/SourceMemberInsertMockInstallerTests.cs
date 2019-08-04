using AutoFake.Expression;
using AutoFake.Setup;
using Moq;
using Xunit;

namespace AutoFake.UnitTests.Setup
{
    public class SourceMemberInsertMockInstallerTests
    {
        private readonly SourceMemberInsertMock _mock;
        private readonly SourceMemberInsertMockInstaller _installer;

        public SourceMemberInsertMockInstallerTests()
        {
            _mock = new SourceMemberInsertMock(Mock.Of<IInvocationExpression>(), null, InsertMock.Location.Top);
            _installer = new SourceMemberInsertMockInstaller(_mock);
        }

        [Fact]
        public void CheckArguments_ReturnsTrue()
        {
            Assert.False(_mock.CheckArguments);
            _installer.CheckArguments();
            Assert.True(_mock.CheckArguments);
        }

        [Fact]
        public void ExpectedCalls_Byte_Success()
        {
            _installer.ExpectedCalls(3);

            Assert.True(_mock.ExpectedCallsFunc(3));
            Assert.False(_mock.ExpectedCallsFunc(2));
        }

        [Fact]
        public void ExpectedCalls_Func_Success()
        {
            _installer.ExpectedCalls(x => x > 2);

            Assert.True(_mock.ExpectedCallsFunc(3));
            Assert.False(_mock.ExpectedCallsFunc(2));
        }
    }
}
