using System.Collections.Generic;
using Xunit;

namespace AutoFake.IntegrationTests
{
    public class InsertTests
    {
        [Fact]
        public void Should_AddNumberInTheEnd_When_Append()
        {
            var fake = new Fake<TestClass>();
            fake.Append(() => TestClass.Numbers.Add(5));
            fake.Rewrite(t => t.SomeMethod());

            fake.Execute(tst =>
            {
                Assert.Empty(TestClass.Numbers);
                tst.SomeMethod();
                Assert.Equal(7, TestClass.Numbers[0]);
                Assert.Equal(5, TestClass.Numbers[1]);
                TestClass.Numbers.Clear();
            });
        }

        [Fact]
        public void Should_AddNumberInTheBeginning_When_Prepend()
        {
            var fake = new Fake<TestClass>();
            fake.Prepend(() => TestClass.Numbers.Add(5));
            fake.Rewrite(t => t.SomeMethod());

            fake.Execute(tst =>
            {
                Assert.Empty(TestClass.Numbers);
                tst.SomeMethod();
                Assert.Equal(5, TestClass.Numbers[0]);
                Assert.Equal(7, TestClass.Numbers[1]);
                TestClass.Numbers.Clear();
            });
        }

        private class TestClass
        {
            //public static List<int> Numbers { get; } = new List<int>();
            public static readonly List<int> Numbers = new List<int>();

            public void SomeMethod()
            {
                Numbers.Add(7);
            }
        }
    }
}
