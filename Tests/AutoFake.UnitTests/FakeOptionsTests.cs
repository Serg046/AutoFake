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

			fakeOptions.AllowedVirtualMembers.Add(m => m.Name == newItem);

			fakeOptions.AllowedVirtualMembers.Should().HaveCount(1);
			fakeOptions.AllowedVirtualMembers.Single()(new MethodContract("", "", newItem, new string[0]))
				.Should().BeTrue();
		}
	}
}
