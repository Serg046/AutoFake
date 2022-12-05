using System;
using FluentAssertions;
using Xunit;

namespace AutoFake.FunctionalTests;

public class PropertyTests
{
	[Fact]
	public void When_no_property_Should_fail()
	{
		Action act1 = () => Property.Of<TestClass>(nameof(TestClass.ReadWriteProperty) + "Saul").Set(() => 5);
		Action act2 = () => Property.Of(typeof(StaticTestClass), nameof(StaticTestClass.ReadWriteProperty) + "Saul").Set(() => 5);

		act1.Should().Throw<MissingMemberException>();
		act2.Should().Throw<MissingMemberException>();
	}

	[Fact]
	public void When_non_static_type_Should_require_generic_version()
	{
		Action act = () => Property.Of(typeof(TestClass), nameof(TestClass.ReadWriteProperty)).Set(() => 5);

		act.Should().Throw<ArgumentException>().WithMessage("*generic version*");
	}

	private class TestClass
	{
		public int ReadWriteProperty { get; set; }

		public void SetReadWriteProperty(int value) => ReadWriteProperty = value;
	}

	private static class StaticTestClass
	{
		public static int ReadWriteProperty { get; set; }

		public static void SetReadWriteProperty(int value) => ReadWriteProperty = value;
	}
}
