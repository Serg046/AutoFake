using AutoFake.Expression;
using AutoFake.Setup;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace AutoFake.UnitTests.Setup
{
    public class RemoveMockInstallerTests
    {
        private readonly ReplaceMock _mock;
        private readonly RemoveMockInstaller _removeMockInstaller;

        public RemoveMockInstallerTests()
        {
            var mocks = new List<IMock>();
            _removeMockInstaller = new RemoveMockInstaller(mocks, Moq.Mock.Of<IInvocationExpression>());
            _mock = (ReplaceMock)mocks.Single();
        }

        [Fact]
        public void CheckArguments_ReturnsTrue()
        {
            Assert.False(_mock.CheckArguments);
            _removeMockInstaller.CheckArguments();
            Assert.True(_mock.CheckArguments);
        }

        [Fact]
        public void ExpectedCalls_Byte_Success()
        {
            _removeMockInstaller.ExpectedCalls(3);

            Assert.True(_mock.ExpectedCallsFunc(3));
            Assert.False(_mock.ExpectedCallsFunc(2));
        }

        [Fact]
        public void ExpectedCalls_Func_Success()
        {
            _removeMockInstaller.ExpectedCalls(x => x > 2);

            Assert.True(_mock.ExpectedCallsFunc(3));
            Assert.False(_mock.ExpectedCallsFunc(2));
        }
    }
}
