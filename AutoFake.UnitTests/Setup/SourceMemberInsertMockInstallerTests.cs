using System;
using System.Collections.Generic;
using AutoFake.Expression;
using AutoFake.Setup.Configurations;
using AutoFake.Setup.Mocks;
using Moq;
using Xunit;

namespace AutoFake.UnitTests.Setup
{
    public class SourceMemberInsertMockInstallerTests
    {
        [Theory, AutoMoqData]
        internal void CheckArguments_ReturnsTrue(SourceMemberInsertMock mock, IProcessorFactory processorFactory)
        {
            mock.CheckArguments = false;
            var sut = new SourceMemberInsertMockConfiguration(mock, processorFactory);

            Assert.False(mock.CheckArguments);
            sut.CheckArguments();

            Assert.True(mock.CheckArguments);
        }

        //[Fact]
        //public void ExpectedCalls_Byte_Success()
        //{
        //    _installer.ExpectedCalls(3);

        //    Assert.True(_mock.ExpectedCallsFunc(3));
        //    Assert.False(_mock.ExpectedCallsFunc(2));
        //}

        [Theory, AutoMoqData]
        internal void ExpectedCalls_Func_Success(SourceMemberInsertMock mock)
        {
            Func<byte, bool> func = x => x > 2;
            var typeInfo = new TypeInfo(func.Method.DeclaringType, new List<FakeDependency>());
            var sut = new SourceMemberInsertMockConfiguration(mock, new ProcessorFactory(typeInfo));

            sut.ExpectedCalls(func);

            Assert.Equal(func.Method.Name, mock.ExpectedCalls.Name);
            Assert.Equal(func.Method.DeclaringType.FullName, mock.ExpectedCalls.DeclaringType);
        }
    }
}
