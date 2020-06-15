using AutoFake.Setup.Configurations;
using AutoFake.Setup.Mocks;
using System;
using System.Collections.Generic;
using Xunit;

namespace AutoFake.UnitTests.Setup
{
    public class ReplaceMockInstallerTests
    {
        [Theory, AutoMoqData]
        internal void CheckArguments_ReturnsTrue(ReplaceMock mock, IProcessorFactory processorFactory)
        {
            mock.CheckArguments = false;
            var sut = new ReplaceMockConfiguration<int>(mock, processorFactory);

            Assert.False(mock.CheckArguments);
            sut.CheckArguments();

            Assert.True(mock.CheckArguments);
        }

        //[Fact]
        //public void ExpectedCalls_Byte_Success()
        //{
        //    _replaceMockInstaller.ExpectedCalls(3);

        //    Assert.True(_mock.ExpectedCallsFunc(3));
        //    Assert.False(_mock.ExpectedCallsFunc(2));
        //}

        [Theory, AutoMoqData]
        internal void ExpectedCalls_Func_Success(ReplaceMock mock)
        {
            Func<byte, bool> func = x => x > 2;
            var typeInfo = new TypeInfo(func.Method.DeclaringType, new List<FakeDependency>());
            var sut = new ReplaceMockConfiguration<int>(mock, new ProcessorFactory(typeInfo));

            sut.ExpectedCalls(func);

            Assert.Equal(func.Method.Name, mock.ExpectedCalls.Name);
            Assert.Equal(func.Method.DeclaringType.FullName, mock.ExpectedCalls.DeclaringType);
        }
    }
}
