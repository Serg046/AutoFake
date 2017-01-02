using Xunit;

namespace AutoFake.UnitTests
{
    public class EqualityArgumentCheckerTests
    {
        [Fact]
        public void Check_Value_Checks()
        {
            var checker = new EqualityArgumentChecker(5);

            Assert.False(checker.Check(-5));
            Assert.True(checker.Check(5));
        }
    }
}
