using System;
using AutoFake.Exceptions;
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

        [Theory]
        [InlineData(true, 2, true)]
        [InlineData(false, 0, false)]
        public void When_ExpectedCallsFunc_ShouldPass(bool equalOp, int arg, bool throws)
        {
            var fake = new Fake<SystemUnderTest>();
            Func<byte, bool> checker;
            if (equalOp) checker = x => x == arg;
            else checker = x => x > arg;


            var sut = fake.Rewrite(f => f.GetCurrentDate());
            sut.Replace(() => DateTime.Now)
                .CheckArguments()
                .ExpectedCalls(checker)
                .Return(DateTime.MaxValue);

            if (throws)
            {
                Assert.Throws<ExpectedCallsException>(() => sut.Execute());
            }
            else
            {
                Assert.Equal(DateTime.MaxValue, sut.Execute());
            }
        }
    }
}
