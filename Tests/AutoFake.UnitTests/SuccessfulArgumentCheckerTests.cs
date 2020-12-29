using Xunit;

namespace AutoFake.UnitTests
{
    public class SuccessfulArgumentCheckerTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData("1")]
        [InlineData(true)]
        public void Check_Argument_True(object arg)
        {
            var sut = new SuccessfulArgumentChecker();

            Assert.True(sut.Check(arg));
        }
    }
}
