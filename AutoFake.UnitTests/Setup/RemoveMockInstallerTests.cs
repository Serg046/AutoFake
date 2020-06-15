using AutoFake.Setup.Configurations;
using AutoFake.Setup.Mocks;
using System;
using System.Collections.Generic;
using Xunit;

namespace AutoFake.UnitTests.Setup
{
    public class RemoveMockInstallerTests
    {
        [Theory, AutoMoqData]
        internal void CheckArguments_ReturnsTrue(ReplaceMock mock)
        {
            mock.CheckArguments = false;
            var sut = new RemoveMockConfiguration(mock);

            Assert.False(mock.CheckArguments);
            sut.CheckArguments();

            Assert.True(mock.CheckArguments);
        }

        [Theory, AutoMoqData]
        internal void ExpectedCalls_Byte_Success(ReplaceMock mock)
        {
            var sut = new ReplaceMockConfiguration<int>(mock);

            sut.ExpectedCalls(3);

            Assert.True(mock.ExpectedCalls(3));
            Assert.False(mock.ExpectedCalls(2));
        }

        [Theory, AutoMoqData]
        internal void ExpectedCalls_Func_Success(ReplaceMock mock)
        {
            Func<byte, bool> func = x => x > 2;
            var sut = new RemoveMockConfiguration(mock);

            sut.ExpectedCalls(func);

            Assert.Equal(func, mock.ExpectedCalls);
        }
    }
}
