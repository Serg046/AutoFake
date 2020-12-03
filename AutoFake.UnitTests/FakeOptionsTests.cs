using System.Linq;
using FluentAssertions;
using Xunit;

namespace AutoFake.UnitTests
{
	public class FakeOptionsTests
	{
		[AutoMoqData, Theory]
		public void VirtualMembers_NewItem_Added(FakeOptions fakeOptions)
		{
			const string newItem = "test";

			fakeOptions.VirtualMembers.Add(newItem);

			fakeOptions.VirtualMembers.Should().HaveCount(1);
			fakeOptions.VirtualMembers.Single().Should().Be(newItem);
		}

		[AutoMoqData, Theory]
		public void IncludeAllVirtualMembers_True_Changed(FakeOptions fakeOptions)
		{
			fakeOptions.IncludeAllVirtualMembers = true;

			fakeOptions.IncludeAllVirtualMembers.Should().BeTrue();
		}
	}
}
