using FluentAssertions;
using System.Diagnostics;
using Xunit;

namespace AutoFake.FunctionalTests.Units
{
	public class FakeOptionsTests
	{
		[Fact]
		public void When_debugger_attached_Should_return_true()
		{
			var fake = new Fake<FakeOptions>();

			var sut = fake.Rewrite(f => f.IsDebugEnabled);
			sut.Replace(() => Debugger.IsAttached).Return(true);

			sut.Execute().Should().BeTrue();
		}
	}
}
