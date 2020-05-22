using Xunit;

namespace AutoFake.IntegrationTests.InstanceTests
{
    public class RewriteTests
    {
        [Fact]
        public void MultipleTestMethodsTest()
        {
            var fake = new Fake<TestClass>();

            fake.Rewrite(m => m.FirstMethod()).Replace(m => m.GetValue()).Return(() => 1);
            fake.Rewrite(m => m.SecondMethod()).Replace(m => m.GetValue()).Return(() => 1);

            fake.Execute(tst =>
            {
                Assert.Equal(1, tst.FirstMethod());
                Assert.Equal(1, tst.SecondMethod());
            });
        }

        [Fact]
        public void ResetTest()
        {
            var fake = new Fake<TestClass>();

            fake.Rewrite(m => m.FirstMethod()).Replace(m => m.GetValue()).Return(() => 1);
            fake.Rewrite(m => m.SecondMethod()).Replace(m => m.GetValue()).Return(() => 2);

            fake.Execute(tst =>
            {
                Assert.Equal(1, tst.FirstMethod());
                Assert.Equal(2, tst.SecondMethod());
            });
        }

        [Fact]
        public void OverloadedMethodTest()
        {
            var fake = new Fake<TestClass>();

            fake.Rewrite(m => m.FirstMethod()).Replace(m => m.GetValue()).Return(() => 1);
            fake.Rewrite(m => m.FirstMethod(Arg.IsAny<int>())).Replace(m => m.GetValue()).Return(() => 2);

            fake.Execute(tst =>
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
