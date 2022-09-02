using AutoFake.Expression;
using FluentAssertions;
using System;
using Xunit;

namespace AutoFake.FunctionalTests.Units
{
	public class GetTestMethodVisitorTests
	{
		[Fact]
		public void When_no_property_getter_Should_fail()
		{
			var visitor = new GetTestMethodVisitor();
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
