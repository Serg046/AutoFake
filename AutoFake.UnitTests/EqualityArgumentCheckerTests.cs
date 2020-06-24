using System.Reflection;
using Xunit;

namespace AutoFake.UnitTests
{
    public class EqualityArgumentCheckerTests
    {
        [Theory]
        [InlineData(5, null, false)]
        [InlineData(5, -5, false)]
        [InlineData(5, 5, true)]
        [InlineData(null, null, true)]
        [InlineData(null, -5, false)]
        [InlineData(null, 5, false)]
        public void Check_Value_Checks(object originalValue, object currentValue, bool flag)
        {
            var checker = new EqualityArgumentChecker(originalValue);

            Assert.Equal(flag, checker.Check(currentValue));
        }

        [Theory]
        [InlineData(null, 0)]
        [InlineData(5, 5)]
        [InlineData(-5, -5)]
        public void GetHashCode_Value_ValueBased(object originalValue, int hash)
        {
            var checker = new EqualityArgumentChecker(originalValue);

            Assert.Equal(hash, checker.GetHashCode());
        }

        [Fact]
        public void ToString_Object_ObjectToString()
        {
            var obj = 5;

            var checker = new EqualityArgumentChecker(obj);

            Assert.Equal("5", checker.ToString());
        }

        [Fact]
        public void ToString_String_WrappedWithQuotes()
        {
            var obj = "5";

            var checker = new EqualityArgumentChecker(obj);

            Assert.Equal("\"5\"", checker.ToString());
        }

        [Fact]
        public void ToString_Null_Represented()
        {
            var checker = new EqualityArgumentChecker(null);

            Assert.Equal("null", checker.ToString());
        }

        [Theory]
        [InlineData(5, null, false)]
        [InlineData(5, -5, false)]
        [InlineData(5, 5, true)]
        [InlineData(null, null, true)]
        [InlineData(null, -5, false)]
        [InlineData(null, 5, false)]
        public void Check_EnumerableEqualityComparer_Checks(object originalValue, object currentValue, bool flag)
        {
            var checker = new EqualityArgumentChecker(new[] {originalValue});

            Assert.Equal(flag, checker.Check(new[] {currentValue}));
        }

        [Theory]
        [InlineData(null)]
        [InlineData(5)]
        [InlineData(-5)]
        public void GetHashCode_EnumerableEqualityComparer_ValueBased(object originalValue)
        {
            var originalEnumerable = new[] {originalValue};
            var checker = new EqualityArgumentChecker(originalEnumerable);

            Assert.Equal(originalEnumerable.GetHashCode(), checker.GetHashCode());
        }

        [Fact]
        public void GetHashCode_EnumerableEqualityComparerWithNull_Zero()
        {
            var checker = new EqualityArgumentChecker(new object[0]);
            var prop = checker.GetType().GetField("_value", BindingFlags.Instance | BindingFlags.NonPublic);
            prop.SetValue(checker, null);

            Assert.Equal(0, checker.GetHashCode());
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData(-5, false)]
        [InlineData(5, false)]
        public void Check_EnumerableEqualityComparerWithNull_Checks(object currentValue, bool flag)
        {
            if (currentValue != null) currentValue = new[] {currentValue};
            var checker = new EqualityArgumentChecker(new object[0]);
            var prop = checker.GetType().GetField("_value", BindingFlags.Instance | BindingFlags.NonPublic);
            prop.SetValue(checker, null);

            Assert.Equal(flag, checker.Check(currentValue));
        }
    }
}
