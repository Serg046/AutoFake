using System.Diagnostics;
using System.IO;
using FluentAssertions;
using Mono.Cecil.Cil;
using Xunit;
using Xunit.Abstractions;

namespace AutoFake.FunctionalTests
{
	public class StateTests
	{
		private readonly ITestOutputHelper _testOutputHelper;

		public StateTests(ITestOutputHelper testOutputHelper)
		{
			_testOutputHelper = testOutputHelper;
		}

		[Fact]
		public void DebugSymbolsTest()
		{
			try
			{
				var fake = new Fake<TestClass>();
				fake.Options.Debug = true;

				var sut = fake.Rewrite(f => f.WriteProperty());
				sut.Replace(f => f.Property).Return(7);

				sut.Execute().Should().Be(7);
				var fileName = typeof(TestClass).Assembly.GetName().Name + "Fake";
				foreach (var file in Directory.GetFiles(".", $"*{fileName}*"))
				{
					File.Delete(file);
				}
			}
			catch (SymbolsNotFoundException)
			{
				_testOutputHelper.WriteLine("No symbols found");
			}
		}

		private class TestClass
		{
			public TestClass()
			{
				Property = 5;
			}

			public int Property { get; }

			public int WriteProperty()
			{
				Debug.WriteLine("Started");
				var value = Property;
				Debug.WriteLine(value);
				return value;
			}
		}
	}
}
