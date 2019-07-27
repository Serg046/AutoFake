using AutoFake.Expression;
using AutoFake.Setup;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace AutoFake.UnitTests.Setup
{
    public class ReplaceMockInstallerTests
    {
        private readonly IMock _mock;
        private readonly ReplaceMockInstaller<int> _replaceMockInstaller;

        public ReplaceMockInstallerTests()
        {
            var mocks = new List<IMock>();
            _replaceMockInstaller = new ReplaceMockInstaller<int>(mocks, Moq.Mock.Of<IInvocationExpression>());
            _mock = mocks.Single();
        }

        [Fact]
        public void CheckArguments_ReturnsTrue()
        {
            Assert.False(_mock.CheckArguments);
            _replaceMockInstaller.CheckArguments();
            Assert.True(_mock.CheckArguments);
        }

        [Fact]
        public void ExpectedCalls_Byte_Success()
        {
            _replaceMockInstaller.ExpectedCalls(3);

            Assert.True(_mock.ExpectedCalls(3));
            Assert.False(_mock.ExpectedCalls(2));
        }

        [Fact]
        public void ExpectedCalls_Func_Success()
        {
            _replaceMockInstaller.ExpectedCalls(x => x > 2);

            Assert.True(_mock.ExpectedCalls(3));
            Assert.False(_mock.ExpectedCalls(2));
        }
    }
}
