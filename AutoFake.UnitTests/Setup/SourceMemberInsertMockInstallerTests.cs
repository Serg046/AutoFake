using AutoFake.Expression;
using AutoFake.Setup.Configurations;
using AutoFake.Setup.Mocks;
using Moq;
using Xunit;

namespace AutoFake.UnitTests.Setup
{
    public class SourceMemberInsertMockInstallerTests
    {
        private readonly SourceMemberInsertMock _mock;
        private readonly SourceMemberInsertMockConfiguration _installer;

        public SourceMemberInsertMockInstallerTests()
        {
            _mock = new SourceMemberInsertMock(new ProcessorFactory(null),
                Mock.Of<IInvocationExpression>(), null, InsertMock.Location.Top);
            _installer = new SourceMemberInsertMockConfiguration(_mock);
        }

        [Fact]
        public void CheckArguments_ReturnsTrue()
        {
            Assert.False(_mock.CheckArguments);
            _installer.CheckArguments();
            Assert.True(_mock.CheckArguments);
        }

        //[Fact]
        //public void ExpectedCalls_Byte_Success()
        //{
        //    _installer.ExpectedCalls(3);

        //    Assert.True(_mock.ExpectedCallsFunc(3));
        //    Assert.False(_mock.ExpectedCallsFunc(2));
        //}

        [Fact]
        public void ExpectedCalls_Func_Success()
        {
            System.Func<byte, bool> func = x => x > 2;

            _installer.ExpectedCalls(func);

            Assert.Equal(func.Method.Name, _mock.ExpectedCalls.Name);
            Assert.Equal(func.Method.DeclaringType.FullName, _mock.ExpectedCalls.DeclaringType);
        }
    }
}
