using Xunit;

namespace AutoFake.IntegrationTests.InstanceTests
{
    public class RewriteTests
    {
        [Fact]
        public void MultipleTestMethodsTest()
        {
            var fake = new Fake<TestClass>();

            fake.Replace(m => m.GetValue())
                .Returns(1);

            fake.Rewrite(m => m.FirstMethod());
            fake.Rewrite(m => m.SecondMethod());

            Assert.Equal(1, fake.Execute(f => f.FirstMethod()));
            Assert.Equal(1, fake.Execute(f => f.SecondMethod()));
        }

        [Fact]
        public void ResetTest()
        {
            var fake = new Fake<TestClass>();

            fake.Replace(m => m.GetValue())
                .Returns(1);
            fake.Rewrite(m => m.FirstMethod());

            fake.Reset();
            fake.Replace(m => m.GetValue())
                .Returns(2);
            fake.Rewrite(m => m.SecondMethod());

            Assert.Equal(1, fake.Execute(f => f.FirstMethod()));
            Assert.Equal(2, fake.Execute(f => f.SecondMethod()));
        }

        [Fact]
        public void OverloadedMethodTest()
        {
            var fake = new Fake<TestClass>();

            fake.Replace(m => m.GetValue())
                .Returns(1);
            fake.Rewrite(m => m.FirstMethod());

            fake.Reset();
            fake.Replace(m => m.GetValue())
                .Returns(2);
            fake.Rewrite(m => m.FirstMethod(Arg.DefaultOf<int>()));

            Assert.Equal(1, fake.Execute(f => f.FirstMethod()));
            Assert.Equal(3, fake.Execute(f => f.FirstMethod(1)));
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
