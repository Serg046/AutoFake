using System;
using System.Collections.Generic;
using AutoFake.Setup.Configurations;
using AutoFake.Setup.Mocks;
using Xunit;

namespace AutoFake.UnitTests.Setup
{
    public class VerifyMockInstallerTests
    {
        [Theory, AutoMoqData]
        internal void CheckArguments_ReturnsTrue(VerifyMock mock, IProcessorFactory processorFactory)
        {
            mock.CheckArguments = false;
            var sut = new VerifyMockConfiguration(mock, processorFactory);

            Assert.False(mock.CheckArguments);
            sut.CheckArguments();

            Assert.True(mock.CheckArguments);
        }

        //[Fact]
        //public void ExpectedCalls_Byte_Success()
        //{
        //    _verifyMockInstaller.ExpectedCalls(3);

        //    Assert.True(_mock.ExpectedCallsFunc(3));
        //    Assert.False(_mock.ExpectedCallsFunc(2));
        //}
        
        [Theory, AutoMoqData]
        internal void ExpectedCalls_Func_Success(VerifyMock mock)
        {
            Func<byte, bool> func = x => x > 2;
            var typeInfo = new TypeInfo(func.Method.DeclaringType, new List<FakeDependency>());
            var sut = new VerifyMockConfiguration(mock, new ProcessorFactory(typeInfo));

            sut.ExpectedCalls(func);

            Assert.Equal(func.Method.Name, mock.ExpectedCalls.Name);
            Assert.Equal(func.Method.DeclaringType.FullName, mock.ExpectedCalls.DeclaringType);
        }
    }
}
