using AutoFake.Abstractions;
using FluentAssertions;
using System;
using Xunit;

namespace AutoFake.FunctionalTests
{
	public class FakeTests
	{
		[Fact]
		public void When_null_as_fake_type_Should_fail()
		{
			Action act = () => new Fake(null);

			act.Should().Throw<ArgumentNullException>();
		}

		[Fact]
		public void When_generic_instance_void_action_Should_execute()
		{
			var fake = new Fake<TestClass>();

			fake.Execute(f => f.SetProp());

			fake.Execute(f => f.Prop).Should().Be(5);
		}

		[Fact]
		public void When_instance_void_action_Should_execute()
		{
			var fake = new Fake(typeof(TestClass));

			fake.Execute((TestClass f) => f.SetProp());

			fake.Execute((TestClass f) => f.Prop).Should().Be(5);
		}

		[Fact]
		public void When_static_void_action_Should_execute()
		{
			var fake = new Fake(typeof(TestClass));

			fake.Execute(() => TestClass.SetStaticProp());

			fake.Execute(() => TestClass.StaticProp).Should().Be(5);
		}

		private class TestClass
		{
			public static int StaticProp { get; private set; }

			public int Prop { get; private set; }

			public void SetProp() => Prop = GetFive();
			public static void SetStaticProp() => StaticProp = GetFive();
			public static int GetFive() => 5;
		}
	}
}
