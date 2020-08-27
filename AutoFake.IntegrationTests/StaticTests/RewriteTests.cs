using Xunit;

namespace AutoFake.IntegrationTests.StaticTests
{
    public class RewriteTests
    {
        [Fact]
        public void MultipleTestMethodsTest()
        {
            var fake = new Fake(typeof(TestClass));

            var sut1 = fake.Rewrite(() => TestClass.FirstMethod()); sut1.Replace(() => TestClass.GetValue()).Return(1);
            var sut2 = fake.Rewrite(() => TestClass.SecondMethod()); sut2.Replace(() => TestClass.GetValue()).Return(1);

            Assert.Equal(1, sut1.Execute());
            Assert.Equal(1, sut2.Execute());
        }

        [Fact]
        public void ResetTest()
        {
            var fake = new Fake(typeof(TestClass));

            var sut1 = fake.Rewrite(() => TestClass.FirstMethod()); sut1.Replace(() => TestClass.GetValue()).Return(1);
            var sut2 = fake.Rewrite(() => TestClass.SecondMethod()); sut2.Replace(() => TestClass.GetValue()).Return(2);

            Assert.Equal(1, sut1.Execute());
            Assert.Equal(2, sut2.Execute());
        }

        [Fact]
        public void OverloadedMethodTest()
        {
            var fake = new Fake(typeof(TestClass));

            var sut1 = fake.Rewrite(() => TestClass.FirstMethod()); sut1.Replace(() => TestClass.GetValue()).Return(1);
            var sut2 = fake.Rewrite(() => TestClass.FirstMethod(1)); sut2.Replace(() => TestClass.GetValue()).Return(2);

            Assert.Equal(1, sut1.Execute());
            Assert.Equal(3, sut2.Execute());
        }

        private static class TestClass
        {
            public static int GetValue() => -1;

            public static int FirstMethod() => GetValue();

            public static int SecondMethod() => GetValue();

            public static int FirstMethod(int arg) => GetValue() + arg;
        }
    }
}
