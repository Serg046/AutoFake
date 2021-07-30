using FluentAssertions;
using Xunit;

namespace AutoFake.FunctionalTests.InstanceTests
{
	public class ConditionalInjectionTests
	{
		[Fact]
		public void When_condition_in_replace_mock_Should_succeed()
		{
			var fake = new Fake<TestClass>();

			var sut = fake.Rewrite(f => f.GetNumber());
			sut.Replace(s => s.GetNumber(5)).Return(7).WhenArgumentsAreMatched();

			sut.Execute().Should().Be(14);
		}

		[Fact]
		public void When_condition_in_remove_mock_Should_succeed()
		{
			var fake = new Fake<TestClass>();

			var sut = fake.Rewrite(f => f.IncNumber());
			sut.Remove(s => s.IncNumber(5)).WhenArgumentsAreMatched();
			sut.Execute();

			fake.Execute(f => f.Number).Should().Be(7);
		}

		private class TestClass
		{
			public int Number { get; private set; }

			public int GetNumber(int number) => number;

			public int GetNumber() => GetNumber(5) + GetNumber(7);

			public void IncNumber(int number) => Number += number;

			public void IncNumber()
			{
				IncNumber(5);
				IncNumber(7);
			}
		}
	}
}
