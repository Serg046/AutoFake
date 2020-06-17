using System;
using Xunit;

namespace AutoFake.UnitTests
{
    public class LambdaArgumentCheckerTests
    {
        [Fact]
        public void Check_Value_Checks()
        {
            var checker = new LambdaArgumentChecker(new Func<object, bool>(x => x.Equals(5)));

            Assert.False(checker.Check(-5));
            Assert.True(checker.Check(5));
        }


        [Fact]
        public void ToString_Object_ObjectToString()
        {
            var checker = new LambdaArgumentChecker(new Func<object, bool>(x => x.Equals(5)));

            Assert.Equal("should match Is-expression", checker.ToString());
        }
    }
}
