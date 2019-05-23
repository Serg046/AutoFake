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
            fake.Rewrite(f => f.Test());

            fake.Execute2(tst => Assert.Throws<InvalidOperationException>(() => tst.Test()));
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
