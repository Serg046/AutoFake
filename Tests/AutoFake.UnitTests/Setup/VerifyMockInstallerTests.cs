using System;
using AutoFake.Setup.Configurations;
using AutoFake.Setup.Mocks;
using Xunit;

namespace AutoFake.UnitTests.Setup
{
    public class VerifyMockInstallerTests
    {
        [Theory, AutoMoqData]
        internal void ExpectedCalls_Byte_Success(VerifyMock mock)
        {
            var sut = new VerifyMockConfiguration(mock);

            sut.ExpectedCalls(3);

            Assert.True(mock.ExpectedCalls(3));
            Assert.False(mock.ExpectedCalls(2));
        }

        [Theory, AutoMoqData]
        internal void ExpectedCalls_Func_Success(VerifyMock mock)
        {
            Func<uint, bool> func = x => x > 2;
            var sut = new VerifyMockConfiguration(mock);

            sut.ExpectedCalls(func);

            Assert.Equal(func, mock.ExpectedCalls);
        }
    }
}
