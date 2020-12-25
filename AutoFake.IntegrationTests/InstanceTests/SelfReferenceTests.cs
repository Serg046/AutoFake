using System;
using FluentAssertions;
using Xunit;

namespace AutoFake.IntegrationTests.InstanceTests
{
	public class SelfReferenceTests
	{
		[Fact]
		public void When_assert_instance_It_should_fail()
		{
			var fake = new Fake<TestClass>();

			Action act = () => fake.Rewrite(f => f.GetReference()).Execute();

			act.Should().Throw<InvalidCastException>().WithMessage("*Cannot cast \"this\" reference to*");
		}

		[Fact]
		public void When_assert_instance_member_It_should_succeed()
		{
			var fake = new Fake<TestClass>();

			fake.Rewrite(f => f.GetReference());

			fake.Execute(f => f.GetReference().Prop).Should().Be(5);
		}

		[Fact]
		public void When_assert_new_instance_without_rewriting_It_should_fail()
		{
			var fake = new Fake<TestClass>();

			Action act = () => fake.Execute(f => f.GetNewReference(7));

			act.Should().Throw<InvalidCastException>().WithMessage("*must be processed by Rewrite*");
		}

		[Fact]
		public void When_assert_new_instance_It_should_succeed()
		{
			var fake = new Fake<TestClass>();

			var sut = fake.Rewrite(f => f.GetNewReference(7));

			var actual = sut.Execute();
			actual.Prop.Should().Be(7);
		}

		[Fact]
		public void When_assert_replaced_instance_It_should_succeed()
		{
			var fake = new Fake<TestClass>();
			var expected = new TestClass {Prop = 4};

			var sut = fake.Rewrite(f => f.GetNewReference(7));
			sut.Replace(() => new TestClass()).Return(expected);

			sut.Execute().Should().BeSameAs(expected);
		}

		public class TestClass
		{
			public int Prop { get; set; } = 5;

			public TestClass GetReference() => this;

			public TestClass GetNewReference(int arg) => new TestClass {Prop = arg};
		}
	}
}
