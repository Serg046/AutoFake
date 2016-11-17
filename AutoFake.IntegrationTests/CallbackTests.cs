using System;
using System.Diagnostics;
using Xunit;

namespace AutoFake.IntegrationTests
{
    public class CallbackTests
    {
        [Fact]
        public void CallbackTest()
        {
            var fake = new Fake<TestClass>();

            fake.Replace(() => Debug.WriteLine(Arg.DefaultOf<int>()))
                .Callback(() => { throw new InvalidOperationException(); });

            Assert.Throws<InvalidOperationException>(() => fake.Rewrite(f => f.Test()).Execute());
        }

        private class TestClass
        {
            public void Test()
            {
                Debug.WriteLine(0);
            }
        }
    }
}
