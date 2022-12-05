using FluentAssertions;
using System;
using Xunit;

namespace AutoFake.FunctionalTests.TypeMemberMocks.InstanceTests;

public class IndexerMockTests
{
	[Fact]
	public void GetterTest()
	{
		var fake = new Fake<TestClass>();

		var sut = fake.Rewrite(f => f.SetAndGetIndexer(1, "2", true, "test"));
		sut.Replace(f => f[1, Arg.IsAny<string>(), true]).Return("newTestValue");

		sut.Execute().Should().Be("newTestValue");
	}

	[Fact]
	public void SetterTest()
	{
		var fake = new Fake<TestClass>();

		var sut = fake.Rewrite(f => f.SetAndGetIndexer(1, "2", true, "test"));
		sut.Remove(Indexer.Of<TestClass>(1, "2", true).Set(() => "test"));

		sut.Execute().Should().Be("0False");
	}

	[Fact]
	public void NoIndexerTest()
	{
		Action act = () => Indexer.Of<TestClass>("set_ItemAndSomeSalt", 1, "2", true).Set(() => "test");

		act.Should().Throw<MissingMemberException>();
	}

	private class TestClass
	{
		private string _indexerValue;
		private int _x; private string _y; private bool _z;

		public string this[int x, string y, bool z]
		{
			get => $"{_indexerValue}{_x}{_y}{_z}";
			set
			{
				_x = x;
				_y = y;
				_z = z;
				_indexerValue = value;
			}
		}

		public string SetAndGetIndexer(int x, string y, bool z, string value)
		{
			this[x, y, z] = value;
			return this[x, y, z];
		}
	}
}
