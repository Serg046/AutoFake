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

            fake.Rewrite(t => t.SomeMethod()).Append(() => TestClass.Numbers.Add(-1));

            fake.Execute(tst =>
            {
                Assert.Empty(TestClass.Numbers);
                tst.SomeMethod();
                Assert.Equal(new[] {3, 5, 7, -1}, TestClass.Numbers);
                TestClass.Numbers.Clear();
            });
        }

        [Fact]
        public void Should_AddNumberInTheBeginning_When_Prepend()
        {
            var fake = new Fake<TestClass>();

            fake.Rewrite(t => t.SomeMethod()).Prepend(() => TestClass.Numbers.Add(-1));

            fake.Execute(tst =>
            {
                Assert.Empty(TestClass.Numbers);
                tst.SomeMethod();
                Assert.Equal(new[] {-1, 3, 5, 7}, TestClass.Numbers);
                TestClass.Numbers.Clear();
            });
        }

        [Fact]
        public void Should_AddNumberAfterCmd_When_AppendWithSourceMember()
        {
            var fake = new Fake<TestClass>();

            fake.Rewrite(t => t.SomeMethod())
                .Append(() => TestClass.Numbers.Add(-1))
                .After((List<int> list) => list.AddRange(new int[0]));

            fake.Execute(tst =>
            {
                Assert.Empty(TestClass.Numbers);
                tst.SomeMethod();
                Assert.Equal(new[] { 3, 5, -1, 7 }, TestClass.Numbers);
                TestClass.Numbers.Clear();
            });
        }

        [Fact]
        public void Should_AddBothNumbers_When_MultipleCallbacks()
        {
            var fake = new Fake<TestClass>();

            var method = fake.Rewrite(t => t.SomeMethod());
            method.Prepend(() => TestClass.Numbers.Add(-1));
            method.Append(() => TestClass.Numbers.Add(-2));

            fake.Execute(tst =>
            {
                Assert.Empty(TestClass.Numbers);
                tst.SomeMethod();
                Assert.Equal(new[] { -1, 3, 5, 7, -2 }, TestClass.Numbers);
                TestClass.Numbers.Clear();
            });
        }

        [Fact]
        public void Should_AddNumberBeforeCmd_When_PrependWithSourceMember()
        {
            var fake = new Fake<TestClass>();

            fake.Rewrite(t => t.SomeMethod())
                .Prepend(() => TestClass.Numbers.Add(-1))
                .Before((List<int> list) => list.AddRange(new int[0])); ;

            fake.Execute(tst =>
            {
                Assert.Empty(TestClass.Numbers);
                tst.SomeMethod();
                Assert.Equal(new[] { 3, -1, 5, 7 }, TestClass.Numbers);
                TestClass.Numbers.Clear();
            });
        }

        private class TestClass
        {
            public static List<int> Numbers { get; } = new List<int>();

            public void SomeMethod()
            {
                Numbers.Add(3);
                Numbers.AddRange(new [] {5});
                Numbers.Add(7);
            }
        }
    }
}
