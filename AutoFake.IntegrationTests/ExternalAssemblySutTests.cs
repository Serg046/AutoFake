using System;
using AutoFake.IntegrationTests.Sut;
using Xunit;

namespace AutoFake.IntegrationTests
{
    public class ExternalAssemblySutTests
    {
        [Fact]
        public void When_SimpleLambda_Should_Pass()
        {
            var fake = new Fake<SystemUnderTest>();

            fake.Execute(f => f.SimpleMethod());
        }

        [Fact]
        public void When_InternalSut_Should_Pass()
        {
            var fake = new Fake<SystemUnderTest>();

            fake.Execute(f => f.InternalMethod());
        }

        [Fact]
        public void When_ExpectedCallsFunc_ShouldPass()
        {
            var fake = new Fake<SystemUnderTest>();

            var sut = fake.Rewrite(f => f.GetCurrentDate());
            sut.Replace(() => DateTime.Now)
                .ExpectedCalls(b => b > 0)
                .Return(DateTime.MaxValue);

            Assert.Equal(DateTime.MaxValue, sut.Execute());
        }
    }
}
