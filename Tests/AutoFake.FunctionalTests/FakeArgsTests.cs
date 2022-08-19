using AutoFake.Exceptions;
using FluentAssertions;
using System;
using Xunit;

namespace AutoFake.FunctionalTests
{
	public class FakeArgsTests
	{
		[Fact]
		public void When_incorrect_args_Should_fail()
		{
			var fake = new Fake<TestClass>(Arg.IsNull<object>());

			Action act = () => fake.Execute(f => f.GetType());

			act.Should().Throw<InitializationException>().WithMessage("Constructor is not found");
		}

		[Fact]
		public void When_ambiguous_null_arg_Should_fail()
		{
			var fake = new Fake<TestClass>(Arg.IsNull<object>(), null);

			Action act = () => fake.Execute(f => f.GetType());

			act.Should().Throw<InitializationException>().WithMessage("Ambiguous null-invocation*");
		}

		private class TestClass
		{
		}
	}
}
