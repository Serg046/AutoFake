using AutoFake.Expression;
using FluentAssertions;
using System;
using Xunit;

namespace AutoFake.FunctionalTests.Units
{
	public class ExpressionTests
	{
		[Fact]
		public void When_GetTestMethodVisitor_without_property_getter_Should_fail()
		{
			var visitor = new GetTestMethodVisitor();
			var prop = typeof(TestClass).GetProperty(nameof(TestClass.PropSetter));

			Action act = () => visitor.Visit(prop);

			act.Should().Throw<NotSupportedException>();
		}

		[Fact]
		public void When_GetSourceMemberVisitor_without_property_getter_Should_fail()
		{
			var visitor = new GetSourceMemberVisitor(m => null, m => null);
			var prop = typeof(TestClass).GetProperty(nameof(TestClass.PropSetter));

			Action act = () => visitor.Visit(prop);

			act.Should().Throw<NotSupportedException>();
		}

		private class TestClass
		{
			public int PropSetter { set { } }
		}
	}
}
