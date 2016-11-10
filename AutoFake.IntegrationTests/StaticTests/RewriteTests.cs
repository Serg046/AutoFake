using Xunit;

namespace AutoFake.IntegrationTests.StaticTests
{
    public class RewriteTests
    {
        [Fact]
        public void MultipleTestMethodsTest()
        {
            var fake = new Fake(typeof(TestClass));

            fake.Replace(() => TestClass.GetValue())
                .Returns(1);

            fake.Rewrite(() => TestClass.FirstMethod());
            fake.Rewrite(() => TestClass.SecondMethod());

            Assert.Equal(1, fake.Execute(() => TestClass.FirstMethod()));
            Assert.Equal(1, fake.Execute(() => TestClass.SecondMethod()));
        }

        [Fact]
        public void ResetTest()
        {
            var fake = new Fake(typeof(TestClass));

            fake.Replace(() => TestClass.GetValue())
                .Returns(1);
            fake.Rewrite(() => TestClass.FirstMethod());

            fake.Reset();
            fake.Replace(() => TestClass.GetValue())
                .Returns(2);
            fake.Rewrite(() => TestClass.SecondMethod());

            Assert.Equal(1, fake.Execute(() => TestClass.FirstMethod()));
            Assert.Equal(2, fake.Execute(() => TestClass.SecondMethod()));
        }

        [Fact]
        public void OverloadedMethodTest()
        {
            var fake = new Fake(typeof(TestClass));

            fake.Replace(() => TestClass.GetValue())
                .Returns(1);
            fake.Rewrite(() => TestClass.FirstMethod());

            fake.Reset();
            fake.Replace(() => TestClass.GetValue())
                .Returns(2);
            fake.Rewrite(() => TestClass.FirstMethod(Arg.DefaultOf<int>()));

            Assert.Equal(1, fake.Execute(() => TestClass.FirstMethod()));
            Assert.Equal(3, fake.Execute(() => TestClass.FirstMethod(1)));
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
