using FluentAssertions;
using Xunit;

namespace AutoFake.FunctionalTests.StaticTests
{
	public class ConditionalInjectionTests
	{
		[Fact]
		public void When_condition_in_replace_mock_Should_succeed()
		{
			var fake = new Fake(typeof(TestClass));

			var sut = fake.Rewrite(() => TestClass.GetNumber());
			sut.Replace(() => TestClass.GetNumber(5)).Return(7).WhenArgumentsAreMatched();

			sut.Execute().Should().Be(14);
		}

		[Fact]
		public void When_condition_in_remove_mock_Should_succeed()
		{
			var fake = new Fake(typeof(TestClass));

			var sut = fake.Rewrite(() => TestClass.IncNumber());
			sut.Remove(() => TestClass.IncNumber(5)).WhenArgumentsAreMatched();
			sut.Execute();

			fake.Execute(() => TestClass.Number).Should().Be(7);
		}

		[Fact]
		public void When_condition_in_prepend_mock_Should_succeed()
		{
			var fake = new Fake(typeof(TestClass));
			var counter = 0;

			var sut = fake.Rewrite(() => TestClass.IncNumber());
			sut.Prepend(() => counter++).Before(() => TestClass.IncNumber(5)).WhenArgumentsAreMatched();
			sut.Execute();

			counter.Should().Be(1);
		}

		[Fact]
		public void When_condition_in_append_mock_Should_succeed()
		{
			var fake = new Fake(typeof(TestClass));
			var counter = 0;

			var sut = fake.Rewrite(() => TestClass.IncNumber());
			sut.Append(() => counter++).After(() => TestClass.IncNumber(5)).WhenArgumentsAreMatched();
			sut.Execute();

			counter.Should().Be(1);
		}

		private static class TestClass
		{
			public static int Number { get; private set; }

			public static int GetNumber(int number) => number;

			public static int GetNumber() => GetNumber(5) + GetNumber(7);

			public static void IncNumber(int number) => Number += number;

			public static void IncNumber()
			{
				IncNumber(5);
				IncNumber(7);
			}
		}
	}
}