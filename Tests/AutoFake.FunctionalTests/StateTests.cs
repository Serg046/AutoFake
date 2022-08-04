using FluentAssertions;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace AutoFake.FunctionalTests
{
	public class StateTests
	{
		[Fact]
		public void When_multiple_mocks_Should_apply_all()
		{
			var enumerable = Enumerable.Range(1, 100);
			var fake = new Fake<TestClass>();

			var i = 0;
			var sut = fake.Rewrite(f => f.Count(enumerable));
			sut.Replace((IEnumerator<int> enumerator) => enumerator.MoveNext()).Return(true).When(() => i++ < 3);
			sut.Replace((IEnumerator<int> enumerator) => enumerator.MoveNext()).Return(false).When(() => i > 3);

			sut.Execute().Should().Be(3);
		}

		private class TestClass
		{
			public int Count(IEnumerable<int> numbers)
			{
				var i = 0;
				foreach (var number in numbers)
				{
					i++;
				}
				return i;
			}
		}
	}
}
