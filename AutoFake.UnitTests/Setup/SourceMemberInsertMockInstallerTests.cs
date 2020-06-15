using System;
using System.Collections.Generic;
using AutoFake.Setup.Configurations;
using AutoFake.Setup.Mocks;
using Xunit;

namespace AutoFake.UnitTests.Setup
{
    public class SourceMemberInsertMockInstallerTests
    {
        [Theory, AutoMoqData]
        internal void CheckArguments_ReturnsTrue(SourceMemberInsertMock mock)
        {
            mock.CheckArguments = false;
            var sut = new SourceMemberInsertMockConfiguration(mock);

            Assert.False(mock.CheckArguments);
            sut.CheckArguments();

            Assert.True(mock.CheckArguments);
        }

        [Theory, AutoMoqData]
        internal void ExpectedCalls_Byte_Success(SourceMemberInsertMock mock)
        {
            var sut = new SourceMemberInsertMockConfiguration(mock);

            sut.ExpectedCalls(3);

            Assert.True(mock.ExpectedCalls(3));
            Assert.False(mock.ExpectedCalls(2));
        }

        [Theory, AutoMoqData]
        internal void ExpectedCalls_Func_Success(SourceMemberInsertMock mock)
        {
            Func<byte, bool> func = x => x > 2;
            var sut = new SourceMemberInsertMockConfiguration(mock);

            sut.ExpectedCalls(func);

            Assert.Equal(func, mock.ExpectedCalls);
        }
    }
}
