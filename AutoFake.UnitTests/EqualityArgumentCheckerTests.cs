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


        [Fact]
        public void ToString_Object_ObjectToString()
        {
            var obj = 5;

            var checker = new EqualityArgumentChecker(obj);

            Assert.Equal("5", checker.ToString());
        }
    }
}
