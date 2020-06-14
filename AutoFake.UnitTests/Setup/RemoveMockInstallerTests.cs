using AutoFake.Expression;
using AutoFake.Setup.Configurations;
using AutoFake.Setup.Mocks;
using System;
using Xunit;

namespace AutoFake.UnitTests.Setup
{
    public class RemoveMockInstallerTests
    {
        private readonly ReplaceMock _mock;
        private readonly RemoveMockConfiguration _removeMockInstaller;

        public RemoveMockInstallerTests()
        {
            _mock = new ReplaceMock(new ProcessorFactory(null), Moq.Mock.Of<IInvocationExpression>());
            _removeMockInstaller = new RemoveMockConfiguration(_mock, default);
        }

        [Fact]
        public void CheckArguments_ReturnsTrue()
        {
            Assert.False(_mock.CheckArguments);
            _removeMockInstaller.CheckArguments();
            Assert.True(_mock.CheckArguments);
        }

        //[Fact]
        //public void ExpectedCalls_Byte_Success()
        //{
        //    _removeMockInstaller.ExpectedCalls(3);

        //    Assert.True(_mock.ExpectedCallsFunc(3));
        //    Assert.False(_mock.ExpectedCallsFunc(2));
        //}

        [Fact]
        public void ExpectedCalls_Func_Success()
        {
            Func<byte, bool> func = x => x > 2;

            _removeMockInstaller.ExpectedCalls(func);

            Assert.Equal(func.Method.Name, _mock.ExpectedCalls.Name);
            Assert.Equal(func.Method.DeclaringType.FullName, _mock.ExpectedCalls.DeclaringType);
        }
    }
}
