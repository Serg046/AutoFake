using FluentAssertions;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace AutoFake.FunctionalTests
{
	public class RewriteTests
	{
		[Fact]
		public void When_null_expression_Should_fail()
		{
			var fake = new Fake<TestClass>();

			Action act = () => fake.Rewrite(null);

			act.Should().Throw<ArgumentNullException>();
		}

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

		[Fact]
		public void When_rewrite_func_with_input_Should_succeed()
		{
			var fake = new Fake(typeof(TestClass));

			var sut1 = fake.Rewrite((TestClass m) => m.FirstMethod()); sut1.Replace((TestClass m) => m.GetValue()).Return(1);
			var sut2 = fake.Rewrite((TestClass m) => m.FirstMethod(1)); sut2.Replace((TestClass m) => m.GetValue()).Return(2);

			Assert.Equal(1, sut1.Execute());
			Assert.Equal(3, sut2.Execute());
		}
		
		[Fact]
		public void When_no_body_Should_fail()
		{
			var fake = new Fake<TestClass>();

			Action act = () => fake.Rewrite(() => TestClass.NoBodyMethod()).Execute();

			act.Should().Throw<NotSupportedException>();
		}

		[Fact]
		public void When_no_method_Should_fail()
		{
			var fake = new Fake<RewriteTests>();

			Action act = () => fake.Rewrite(() => TestClass.NoBodyMethod()).Execute();

			act.Should().Throw<MissingMethodException>();
		}

		[Fact]
		public void When_no_property_Should_fail()
		{
			var fake = new Fake<RewriteTests>();

			Action act = () => fake.Execute((TestClass f) => f.Date);

			act.Should().Throw<MissingMemberException>();
		}

		[Fact]
		public void When_no_field_Should_fail()
		{
			var fake = new Fake<RewriteTests>();

			Action act = () => fake.Execute(() => TextReader.Null);

			act.Should().Throw<MissingMemberException>();
		}

		[Fact]
		public void When_rewrite_ctor_Should_succeed()
		{
			var fake = new Fake<TestClass>();
			var date = new DateTime(2022, 9, 3);

			var sut = fake.Rewrite(() => new TestClass());
			sut.Replace(() => DateTime.Now).Return(date);

			fake.Execute(f => f.Date).Should().Be(date);
		}

		private class TestClass
		{
			public DateTime Date { get; } = DateTime.Now;

			public int GetValue() => -1;

			public int FirstMethod() => GetValue();

			public int SecondMethod() => GetValue();

			public int FirstMethod(int arg) => GetValue() + arg;

			[DllImport("no.dll")]
			public static extern void NoBodyMethod();
		}
	}
}
