using FluentAssertions;
using System.Linq;
using Xunit;

namespace AutoFake.FunctionalTests.Units
{
	public class ExtensionsTests
	{
		[Fact]
		public void When_no_generic_args_Should_return_null()
		{
			Extensions.FindGenericTypeOrDefault(Enumerable.Empty<GenericArgument>(), string.Empty)
				.Should().BeNull();
		}
	}
}
