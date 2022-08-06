﻿using Xunit;

namespace AutoFake.FunctionalTests
{
	public class RewriteTests
	{
		[Fact]
		public void When_multiple_suts_Should_succeed()
		{
			var fake = new Fake<TestClass>();

			var sut1 = fake.Rewrite(m => m.FirstMethod()); sut1.Replace(m => m.GetValue()).Return(1);
			var sut2 = fake.Rewrite(m => m.SecondMethod()); sut2.Replace(m => m.GetValue()).Return(1);

			Assert.Equal(1, sut1.Execute());
			Assert.Equal(1, sut2.Execute());
		}

		[Fact]
		public void When_overloaded_methods_Should_choose_the_right()
		{
			var fake = new Fake<TestClass>();

			var sut1 = fake.Rewrite(m => m.FirstMethod()); sut1.Replace(m => m.GetValue()).Return(1);
			var sut2 = fake.Rewrite(m => m.FirstMethod(1)); sut2.Replace(m => m.GetValue()).Return(2);

			Assert.Equal(1, sut1.Execute());
			Assert.Equal(3, sut2.Execute());
		}

		private class TestClass
		{
			public int GetValue() => -1;

			public int FirstMethod() => GetValue();

			public int SecondMethod() => GetValue();

			public int FirstMethod(int arg) => GetValue() + arg;

		}
	}
}