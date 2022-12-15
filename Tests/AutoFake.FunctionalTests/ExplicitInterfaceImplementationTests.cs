using FluentAssertions;
using Xunit;

namespace AutoFake.FunctionalTests;

public class ExplicitInterfaceImplementationTests
{
	[Fact]
	public void When_rewrite_method_Should_succeed()
	{
		var fake = new Fake<TestClass>();

		fake.Rewrite((ITest f) => f.GetFive()).Execute().Should().Be(5);
	}

	[Fact]
	public void When_rewrite_generic_method_Should_succeed()
	{
		var fake = new Fake<TestClass<int>>();

		fake.Rewrite((ITest<int> f) => f.GetValue<double>(1, 1.0)).Execute().Should().Be((1, 1.0));
	}

	[Fact]
	public void When_replace_method_Should_succeed()
	{
		var fake = new Fake<TestClass>();

		var sut = fake.Rewrite(f => f.CallGetFive());
		sut.Replace((ITest f) => f.GetFive()).Return(7);
			
		sut.Execute().Should().Be(7);
	}

	[Fact]
	public void When_replace_generic_method_Should_succeed()
	{
		var fake = new Fake<TestClass<int>>();

		var sut = fake.Rewrite(f => f.CallGetValue(1, 1.0));
		sut.Replace((ITest<int> f) => f.GetValue<double>(1, 1.0)).Return((7, 7.0));
		
		sut.Execute().Should().Be((7, 7.0));
	}

	private interface ITest
	{
		int GetFive();
	}

	private interface ITest<T1>
	{
		(T1, T2) GetValue<T2>(T1 value1, T2 value2);
	}

	private class TestClass : ITest
	{
		public int CallGetFive() => ((ITest)this).GetFive();
		int ITest.GetFive() => 5;
	}

	private class TestClass<T1> : ITest<T1>
	{
		public (T1, T2) CallGetValue<T2>(T1 value1, T2 value2) => ((ITest<T1>)this).GetValue(value1, value2);
		(T1, T2) ITest<T1>.GetValue<T2>(T1 value1, T2 value2) => (value1, value2);
	}
}
