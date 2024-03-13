using System;
using FluentAssertions;
using Xunit;

namespace AutoFake.FunctionalTests;

public class RemoveTests
{
	[ExcludedTheory]
	[InlineData(0, 0, 1, false)]
	[InlineData(1, 2, 2, false)]
	[InlineData(0, 0, 2, true)]
	[InlineData(1, 2, 3, true)]
	public void When_expected_calls_configured_Should_check(int a, int b, uint expectedCalls, bool fails)
	{
		var fake = new Fake<TestClass>();

		var sut = fake.Rewrite(f => f.Sum(a, b));
		sut.Remove(f => f.CodeBranch(a, b)).ExpectedCalls(expectedCalls);

		Action act = () => sut.Execute();
		if (fails) act.Should().Throw<MethodAccessException>(); else act.Should().NotThrow();
	}

	[ExcludedFact]
	public void When_arguments_are_matched_Should_remove()
	{
		var fake = new Fake<TestClass>();

		var sut = fake.Rewrite(f => f.Sum(1, 2));
		sut.Remove(f => f.CodeBranch(1, 2)).WhenArgumentsAreMatched();

		sut.Execute();
		fake.Execute(f => f.Field).Should().Be(0);
	}

	[ExcludedFact]
	public void When_arguments_are_not_matched_Should_not_remove()
	{
		var fake = new Fake<TestClass>();

		var sut = fake.Rewrite(f => f.Sum(1, 2));
		sut.Remove(f => f.CodeBranch(1, 3)).WhenArgumentsAreMatched();

		sut.Execute();
		fake.Execute(f => f.Field).Should().Be(6);
	}

	[ExcludedTheory]
	[InlineData(true, 0)]
	[InlineData(false, 6)]
	public void When_condition_is_provided_Should_check(bool condition, int result)
	{
		var fake = new Fake<TestClass>();

		var sut = fake.Rewrite(f => f.Sum(1, 2));
		sut.Remove(f => f.CodeBranch(1, 2)).When(() => condition);

		sut.Execute();
		fake.Execute(f => f.Field).Should().Be(result);
	}

	[ExcludedTheory]
	[InlineData(0, 0)]
	[InlineData(1, 7)]
	public void When__fake_condition_is_provided_Should_check(int initValue, int result)
	{
		var fake = new Fake<TestClass>(initValue);

		var sut = fake.Rewrite(f => f.Sum(1, 2));
		sut.Remove(f => f.CodeBranch(1, 2)).When(e => e.Execute(f => f.Field) == 0);

		sut.Execute();
		fake.Execute(f => f.Field).Should().Be(result);
	}

	private class TestClass
	{
		public int Field;

		public TestClass() { }
		public TestClass(int field) => Field = field;

		public void CodeBranch(int a, int b) => Field += a + b;

		public void Sum(int a, int b)
		{
			if (a > 0)
			{
				CodeBranch(a, b);
				CodeBranch(a, b);
			}
			else
			{
				CodeBranch(0, 0);
			}
		}
	}
}
