using System;
using Xunit;

namespace AutoFake.IntegrationTests
{
    public class CallbackTests
    {
        [Fact]
        public void CallbackTest()
        {
            var fake = new Fake<TestClass>();

            fake.Replace(() => Console.WriteLine(Arg.DefaultOf<int>()))
                .Callback(() => throw new InvalidOperationException());

            Assert.Throws<InvalidOperationException>(() => fake.Rewrite(f => f.Test()).Execute());
        }

        private class TestClass
        {
            public void Test()
            {
                Console.WriteLine(0);
            }
        }
    }
}
