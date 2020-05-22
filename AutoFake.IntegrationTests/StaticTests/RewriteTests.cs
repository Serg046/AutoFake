using Xunit;

namespace AutoFake.IntegrationTests.StaticTests
{
    public class RewriteTests
    {
        [Fact]
        public void MultipleTestMethodsTest()
        {
            var fake = new Fake(typeof(TestClass));

            fake.Rewrite(() => TestClass.FirstMethod()).Replace(() => TestClass.GetValue()).Return(() => 1);
            fake.Rewrite(() => TestClass.SecondMethod()).Replace(() => TestClass.GetValue()).Return(() => 1);

            fake.Execute(() =>
            {
                Assert.Equal(1, TestClass.FirstMethod());
                Assert.Equal(1, TestClass.SecondMethod());
            });
        }

        [Fact]
        public void ResetTest()
        {
            var fake = new Fake(typeof(TestClass));

            fake.Rewrite(() => TestClass.FirstMethod()).Replace(() => TestClass.GetValue()).Return(() => 1);
            fake.Rewrite(() => TestClass.SecondMethod()).Replace(() => TestClass.GetValue()).Return(() => 2);

            fake.Execute(() =>
            {
                Assert.Equal(1, TestClass.FirstMethod());
                Assert.Equal(2, TestClass.SecondMethod());
            });
        }

        [Fact]
        public void OverloadedMethodTest()
        {
            var fake = new Fake(typeof(TestClass));

            fake.Rewrite(() => TestClass.FirstMethod()).Replace(() => TestClass.GetValue()).Return(() => 1);
            fake.Rewrite(() => TestClass.FirstMethod(Arg.IsAny<int>())).Replace(() => TestClass.GetValue()).Return(() => 2);

            fake.Execute(() =>
            {
                Assert.Equal(1, TestClass.FirstMethod());
                Assert.Equal(3, TestClass.FirstMethod(1));
            });
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
