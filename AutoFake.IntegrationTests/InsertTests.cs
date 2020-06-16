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
            var numbers = new List<int>();

            var sut = fake.Rewrite(t => t.SomeMethod(numbers));
            sut.Append(() => numbers.Add(-1));

            sut.Execute();
            Assert.Equal(new[] {3, 5, 7, -1}, numbers);
        }

        [Fact]
        public void Should_AddNumberInTheBeginning_When_Prepend()
        {
            var fake = new Fake<TestClass>();
            var numbers = new List<int>();

            var sut = fake.Rewrite(t => t.SomeMethod(numbers));
            sut.Prepend(() => numbers.Add(-1));

            sut.Execute();
            Assert.Equal(new[] { -1, 3, 5, 7 }, numbers);
        }

        [Fact]
        public void Should_AddNumberAfterCmd_When_AppendWithSourceMember()
        {
            var fake = new Fake<TestClass>();
            var numbers = new List<int>();

            var sut = fake.Rewrite(t => t.SomeMethod(numbers));
            sut.Append(() => numbers.Add(-1))
                .After((List<int> list) => list.AddRange(new int[0]));

            sut.Execute();
            Assert.Equal(new[] { 3, 5, -1, 7 }, numbers);
        }

        [Fact]
        public void Should_AddBothNumbers_When_MultipleCallbacks()
        {
            var fake = new Fake<TestClass>();
            var numbers = new List<int>();

            var sut = fake.Rewrite(t => t.SomeMethod(numbers));
            sut.Prepend(() => numbers.Add(-1));
            sut.Append(() => numbers.Add(-2));

            sut.Execute();
            Assert.Equal(new[] { -1, 3, 5, 7, -2 }, numbers);
        }

        [Fact]
        public void Should_AddNumberBeforeCmd_When_PrependWithSourceMember()
        {
            var fake = new Fake<TestClass>();
            var numbers = new List<int>();

            var sut = fake.Rewrite(t => t.SomeMethod(numbers));
            sut.Prepend(() => numbers.Add(-1))
                .Before((List<int> list) => list.AddRange(new int[0]));

            sut.Execute();
            Assert.Equal(new[] { 3, -1, 5, 7 }, numbers);
        }

        [Fact]
        public void Should_AddNumberToLocalVar_When_Append()
        {
            var numbers = new List<int>();
            var fake = new Fake<TestClass>();

            var sut = fake.Rewrite(t => t.SomeMethod(new List<int>()));
            sut.Append(() => numbers.Add(-1));

            sut.Execute();
            Assert.Equal(new[] { -1 }, numbers);
        }

        private class TestClass
        {
            public void SomeMethod(List<int> numbers)
            {
                numbers.Add(3);
                numbers.AddRange(new [] {5});
                numbers.Add(7);
            }
        }
    }
}
