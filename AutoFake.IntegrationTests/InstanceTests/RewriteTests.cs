using Xunit;

namespace AutoFake.IntegrationTests.InstanceTests
{
    public class RewriteTests
    {
        [Fact]
        public void MultipleTestMethodsTest()
        {
            var fake = new Fake<TestClass>();
            fake.Options.AutoDisposal = false;

            var sut1 = fake.Rewrite(m => m.FirstMethod()); sut1.Replace(m => m.GetValue()).Return(1);
            var sut2 = fake.Rewrite(m => m.SecondMethod()); sut2.Replace(m => m.GetValue()).Return(1);

            Assert.Equal(1, sut1.Execute());
            Assert.Equal(1, sut2.Execute());
            fake.Release();
        }

        [Fact]
        public void ResetTest()
        {
            var fake = new Fake<TestClass>();
            fake.Options.AutoDisposal = false;

            var sut1 = fake.Rewrite(m => m.FirstMethod()); sut1.Replace(m => m.GetValue()).Return(1);
            var sut2 = fake.Rewrite(m => m.SecondMethod()); sut2.Replace(m => m.GetValue()).Return(2);

            Assert.Equal(1, sut1.Execute());
            Assert.Equal(2, sut2.Execute());
            fake.Release();
        }

        [Fact]
        public void OverloadedMethodTest()
        {
            var fake = new Fake<TestClass>();
            fake.Options.AutoDisposal = false;

            var sut1 = fake.Rewrite(m => m.FirstMethod()); sut1.Replace(m => m.GetValue()).Return(1);
            var sut2 = fake.Rewrite(m => m.FirstMethod(1)); sut2.Replace(m => m.GetValue()).Return(2);

            Assert.Equal(1, sut1.Execute());
            Assert.Equal(3, sut2.Execute());
            fake.Release();
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
