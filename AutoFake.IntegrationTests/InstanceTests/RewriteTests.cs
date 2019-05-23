using Xunit;

namespace AutoFake.IntegrationTests.InstanceTests
{
    public class RewriteTests
    {
        [Fact]
        public void MultipleTestMethodsTest()
        {
            var fake = new Fake<TestClass>();

            fake.Replace(m => m.GetValue()).Returns(() => 1);
            fake.Rewrite(m => m.FirstMethod());
            fake.Rewrite(m => m.SecondMethod());

            fake.Execute2(tst =>
            {
                Assert.Equal(1, tst.FirstMethod());
                Assert.Equal(1, tst.SecondMethod());
            });
        }

        [Fact]
        public void ResetTest()
        {
            var fake = new Fake<TestClass>();

            fake.Replace(m => m.GetValue()).Returns(() => 1);
            fake.Rewrite(m => m.FirstMethod());

            fake.Reset();
            fake.Replace(m => m.GetValue()).Returns(() => 2);
            fake.Rewrite(m => m.SecondMethod());

            fake.Execute2(tst =>
            {
                Assert.Equal(1, tst.FirstMethod());
                Assert.Equal(2, tst.SecondMethod());
            });
        }

        [Fact]
        public void OverloadedMethodTest()
        {
            var fake = new Fake<TestClass>();

            fake.Replace(m => m.GetValue()).Returns(() => 1);
            fake.Rewrite(m => m.FirstMethod());

            fake.Reset();
            fake.Replace(m => m.GetValue()).Returns(() => 2);
            fake.Rewrite(m => m.FirstMethod(Arg.DefaultOf<int>()));

            fake.Execute2(tst =>
            {
                Assert.Equal(1, tst.FirstMethod());
                Assert.Equal(3, tst.FirstMethod(1));
            });
        }

        private class TestClass
        {
            public int GetValue() => -1;

            public int FirstMethod() => GetValue();

            public int SecondMethod() => GetValue();

            public int FirstMethod(int arg) => GetValue() + arg;

        }
    }
}
