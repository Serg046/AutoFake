using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace AutoFake.FunctionalTests
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
                .After((List<int> list) => list.AddRange(Arg.IsAny<int[]>()));

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
                .Before((List<int> list) => list.AddRange(Arg.IsAny<int[]>()));

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

        [Fact]
        public void Should_add_numbers_to_the_appropriate_places_When_generic_insert_mocks()
        {
            var numbers = new List<int>();
            var fake = new Fake<TestClass>();
            var sut = fake.Rewrite(t => t.SomeMethod(numbers));
            sut.Prepend(() => numbers.Add(-6)).Before(t => t.AnotherMethod());
            sut.Append(() => numbers.Add(6)).After(t => t.AnotherMethod());

            sut.Execute();

            numbers.Should().ContainInOrder(3, 5, -6, 6, 7);
        }

		[Fact]
        public void Should_add_numbers_When_arguments_are_matched()
		{
            var numbers = new List<int>();
            var fake = new Fake<TestClass>();

            var sut = fake.Rewrite(f => f.SomeMethod(numbers));
            sut.Append(() => numbers.Add(0)).After((List<int> list) => list.Add(Arg.IsAny<int>()));
            sut.Append(() => numbers.Add(-1)).After((List<int> list) => list.Add(3)).WhenArgumentsAreMatched();
            sut.Append(() => numbers.Add(-2)).After((List<int> list) => list.Add(7)).WhenArgumentsAreMatched();

            sut.Execute();
            numbers.Should().ContainInOrder(3, -1, 0, 5, 7, -2, 0);
        }

        [Fact]
        public void Should_add_numbers_When_invariants_are_matched()
        {
            var numbers = new List<int>();
            var fake = new Fake<TestClass>();

            var sut = fake.Rewrite(f => f.SomeMethod(numbers));
            sut.Append(() => numbers.Add(0)).After((List<int> list) => list.Add(Arg.IsAny<int>()));
            sut.Append(() => numbers.Add(-1)).After((List<int> list) => list.Add(Arg.IsAny<int>()))
                .When(f => f.Execute(x => x.Prop) == 0);
            sut.Append(() => numbers.Add(-2)).After((List<int> list) => list.Add(Arg.IsAny<int>()))
                .When(f => f.Execute(x => x.Prop) == 1);

            sut.Execute();
            numbers.Should().ContainInOrder(3, -1, 0, 5, 7, -2, 0);
        }

        private class TestClass
        {
            public int Prop { get; private set; }

            public void SomeMethod(List<int> numbers)
            {
                numbers.Add(3);
                numbers.AddRange(new [] {5});
                AnotherMethod();
                numbers.Add(7);
            }

            public void AnotherMethod()
            {
                Prop = 1;
            }
        }
    }
}
