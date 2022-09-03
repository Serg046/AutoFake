using FluentAssertions;
using System;
using System.Reflection;
using Xunit;

namespace AutoFake.FunctionalTests
{
	public class ArgumentTests
	{
		[Fact]
		public void When_incorrect_args_Should_fail()
		{
			var fake = new Fake<TestClass>(Arg.IsNull<object>());

			Action act = () => fake.Execute(f => f.GetType());

			act.Should().Throw<MissingMethodException>().WithMessage("Constructor is not found");
		}

		[Fact]
		public void When_ambiguous_null_arg_Should_fail()
		{
			var fake = new Fake<TestClass>(Arg.IsNull<object>(), null);

			Action act = () => fake.Execute(f => f.GetType());

			act.Should().Throw<AmbiguousMatchException>().WithMessage("Ambiguous null-invocation*");
		}

		[Fact]
		public void When_method_call_as_arg_Should_Succeed()
		{
			var fake = new Fake<TestClass>();
			var testClass = new TestClass();

			var sut = fake.Rewrite(f => f.GetNumber("1"));
			sut.Verify(f => f.SomeMethod(GetOne()));

			sut.Execute();
		}

		[Fact]
		public void When_is_arg_with_different_type_Should_Succeed()
		{
			var fake = new Fake<TestClass>();
			var testClass = new TestClass();

			var sut = fake.Rewrite(f => f.GetNumber("1"));
			sut.Verify(f => f.SomeMethod(Arg.Is<string>(x => x == "1")));

			sut.Execute();
		}

		string GetOne() => "1";

		private class TestClass
		{
			public string GetNumber(string arg) => SomeMethod(arg);
			public string SomeMethod(object arg) => (string)arg;
		}
	}
}
