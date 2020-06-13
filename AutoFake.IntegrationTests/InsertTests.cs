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

            var sut = fake.Rewrite(t => t.SomeMethod());
            sut.Append(() => TestClass.Numbers.Add(-1));

            Assert.Empty(TestClass.Numbers);
            sut.Execute();
            Assert.Equal(new[] {3, 5, 7, -1}, TestClass.Numbers);
            TestClass.Numbers.Clear();
        }

        [Fact]
        public void Should_AddNumberInTheBeginning_When_Prepend()
        {
            var fake = new Fake<TestClass>();

            var sut = fake.Rewrite(t => t.SomeMethod());
            sut.Prepend(() => TestClass.Numbers.Add(-1));

            Assert.Empty(TestClass.Numbers);
            sut.Execute();
            Assert.Equal(new[] {-1, 3, 5, 7}, TestClass.Numbers);
            TestClass.Numbers.Clear();
        }

        [Fact]
        public void Should_AddNumberAfterCmd_When_AppendWithSourceMember()
        {
            var fake = new Fake<TestClass>();

            var sut = fake.Rewrite(t => t.SomeMethod());
            sut.Append(() => TestClass.Numbers.Add(-1))
                .After((List<int> list) => list.AddRange(new int[0]));

            Assert.Empty(TestClass.Numbers);
            sut.Execute();
            Assert.Equal(new[] { 3, 5, -1, 7 }, TestClass.Numbers);
            TestClass.Numbers.Clear();
        }

        [Fact]
        public void Should_AddBothNumbers_When_MultipleCallbacks()
        {
            var fake = new Fake<TestClass>();

            var sut = fake.Rewrite(t => t.SomeMethod());
            sut.Prepend(() => TestClass.Numbers.Add(-1));
            sut.Append(() => TestClass.Numbers.Add(-2));

            Assert.Empty(TestClass.Numbers);
            sut.Execute();
            Assert.Equal(new[] { -1, 3, 5, 7, -2 }, TestClass.Numbers);
            TestClass.Numbers.Clear();
        }

        [Fact]
        public void Should_AddNumberBeforeCmd_When_PrependWithSourceMember()
        {
            var fake = new Fake<TestClass>();

            var sut = fake.Rewrite(t => t.SomeMethod());
            sut.Prepend(() => TestClass.Numbers.Add(-1))
                .Before((List<int> list) => list.AddRange(new int[0])); ;

            Assert.Empty(TestClass.Numbers);
            sut.Execute();
            Assert.Equal(new[] { 3, -1, 5, 7 }, TestClass.Numbers);
            TestClass.Numbers.Clear();
        }

        [Fact]
        public void Should_AddNumberToLocalVar_When_Append()
        {
            var numbers = new List<int>();
            var fake = new Fake<TestClass>();

            var sut = fake.Rewrite(t => t.SomeMethod());
            sut.Append(() => numbers.Add(-1));

            sut.Execute();
            Assert.Equal(new[] { -1 }, numbers);
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
