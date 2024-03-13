using FluentAssertions;
using Xunit;

namespace AutoFake.FunctionalTests.Contract
{
	public class ArgLambdaTests
	{
		[ExcludedFact]
		public void When_arg_is_custom_type_Should_succeed()
		{
			var fake = new Fake<TestClass>();

			var sut = fake.Rewrite(f => f.SomeMethod());
			sut.Replace(f => f.SomeMethod(Arg.Is<HelperClass>(arg => arg.Prop == 6))).Return(2);

			sut.Execute().Should().Be(2);
		}

		[ExcludedFact]
		public void When_int_is_captured_Should_pass()
		{
			var fake = new Fake<TestClass>();
			var propValue = 6;

			var sut = fake.Rewrite(f => f.SomeMethod());
			sut.Replace(f => f.SomeMethod(Arg.Is<HelperClass>(arg => arg.Prop == propValue))).Return(2);

			sut.Execute().Should().Be(2);
		}

		[ExcludedFact]
		public void When_custom_type_is_captured_Should_pass()
		{
			var fake = new Fake<TestClass>();
			var helper = new HelperClass { Prop = 6 };

			var sut = fake.Rewrite(f => f.SomeMethod());
			sut.Replace(f => f.SomeMethod(Arg.Is<HelperClass>(arg => arg.Prop == helper.Prop))).Return(2);

			sut.Execute().Should().Be(2);
		}

		[ExcludedFact]
		public void When_custom_interface_is_captured_Should_pass()
		{
			var fake = new Fake<TestClass>();
			IHelperClass helper = new HelperClass { Prop = 6 };

			var sut = fake.Rewrite(f => f.SomeInterfaceMethod());
			sut.Replace(f => f.SomeInterfaceMethod(Arg.Is<HelperClass>(arg => arg.Prop == helper.Prop))).Return(2);

			sut.Execute().Should().Be(2);
		}

		[ExcludedFact]
		public void When_custom_is_captured_and_compared_Should_pass()
		{
			var fake = new Fake<TestClass>();
			IHelperClass helper = new HelperClass { Prop = 6 };

			var sut = fake.Rewrite(f => f.SomeInterfaceMethod());
			sut.Replace(f => f.SomeInterfaceMethod(Arg.Is<HelperClass>(arg => arg.Equals(helper)))).Return(2);

			sut.Execute().Should().Be(2);
		}

		public interface IHelperClass
		{
			public int Prop { get; set; }
		}

		public class HelperClass : IHelperClass
		{
			public int Prop { get; set; }

			public override bool Equals(object obj) => obj is HelperClass h && h.Prop == Prop;

			public override int GetHashCode() => Prop.GetHashCode();
		}

		private class TestClass
		{
			public int SomeMethod() => SomeMethod(new HelperClass { Prop = 6 });
			public int SomeInterfaceMethod() => SomeInterfaceMethod(new HelperClass { Prop = 6 });
			public int SomeMethod(HelperClass h) => h.Prop;
			public int SomeInterfaceMethod(IHelperClass h) => h.Prop;
		}
	}
}
