using System;
using System.Diagnostics;
using AutoFake.Abstractions;
using FluentAssertions;
using Xunit;

namespace AutoFake.FunctionalTests
{
	public class ReplaceTests
	{
		[Theory]
		[InlineData(false, false, true)]
		[InlineData(true, false, true)]
		[InlineData(false, true, true)]
		[InlineData(true, true, false)]
		public void When_arguments_are_passed_Should_be_checked(bool correctDate, bool correctZone, bool throws)
		{
			var fake = new Fake<TestClass>();
			var date = correctDate ? new DateTime(2019, 1, 1) : DateTime.MinValue;
			var zone = correctZone ? TimeZoneInfo.Utc
				: TimeZoneInfo.CreateCustomTimeZone("incorrect", TimeSpan.FromHours(-6), "", "");

			var sut = fake.Rewrite(f => f.GetValueByArguments(date, zone));
			sut.Replace(() => TimeZoneInfo.ConvertTimeFromUtc(new DateTime(2019, 1, 1), TimeZoneInfo.Utc))
				.Return(DateTime.MinValue);

			if (throws)
			{
				Assert.Throws<ArgumentException>(() => sut.Execute());
			}
			else
			{
				sut.Execute();
			}
		}

		[Theory]
		[InlineData(true, 2, true)]
		[InlineData(true, 1, false)]
		[InlineData(false, 0, false)]
		public void When_expected_calls_configured_Should_check(bool equalOp, int arg, bool throws)
		{
			var fake = new Fake<TestClass>();
			IExecutionContext.CallsCheckerFunc checker;
			if (equalOp) checker = x => x == arg;
			else checker = x => x > arg;

			var sut = fake.Rewrite(f => f.GetValueByArguments(DateTime.MinValue, null));
			sut.Replace(() => TimeZoneInfo.ConvertTimeFromUtc(default, default))
				.ExpectedCalls(checker)
				.Return(DateTime.MinValue);

			if (throws)
			{
				Assert.Throws<MethodAccessException>(() => sut.Execute());
			}
			else
			{
				Assert.Equal(DateTime.MinValue, sut.Execute());
			}
		}

		[Fact]
		public void When_there_are_branches_Should_pass()
		{
			var fake = new Fake<TestClass>();
			var sut = fake.Rewrite(f => f.Sum(1, 2));
			sut.Replace(t => t.CodeBranch(1, 2))
				.ExpectedCalls(2)
				.Return(6);

			Assert.Equal(12, sut.Execute());

			fake = new Fake<TestClass>();
			sut = fake.Rewrite(f => f.Sum(0, 1));
			sut.Replace(t => t.CodeBranch(0, 0))
				.ExpectedCalls(1)
				.Return(6);

			Assert.Equal(6, sut.Execute());
		}

		[Fact]
		public void When_source_assembly_instance_Should_succeed()
		{
			var date = new DateTime(2020, 5, 23);
			var fake = new Fake<TestClass>();

			var sut = fake.Rewrite(f => f.GetValueByArguments(new DateTime(2019, 1, 1), TimeZoneInfo.Utc));
			sut.Replace(() => TimeZoneInfo.ConvertTimeFromUtc(new DateTime(2019, 1, 1), TimeZoneInfo.Utc))
				.Return(date);

			Assert.Equal(date, sut.Execute());
		}

		[Fact]
		public void When_replace_some_instance_Should_succeed()
		{
			const int mutator = 4;
			var cl2 = new TestClass2 { Value = 2 };
			var t1 = new TestClass2();
			var fake = new Fake<TestClass>();

			var sut = fake.Rewrite(f => f.MutateTestClass2(t1, mutator));
			sut.Replace(f => f.Mutator(Arg.IsAny<TestClass2>(), mutator))
				.Return(cl2);

			Assert.Equal(cl2, sut.Execute());
		}

		[Fact]
		public void When_different_generic_args_Should_not_replace()
		{
			var fake = new Fake<TestClass<int>>(5);
			var sut = fake.Rewrite(f => f.GetValue());
			sut.Replace(t => t.Field).Return(7);
			sut.Execute().Should().Be(7);

			fake = new Fake<TestClass<int>>(5);
			sut = fake.Rewrite(f => f.GetValue());
			sut.Replace((TestClass<double> t) => t.Field).Return(7);
			sut.Execute().Should().Be(5);
		}

		[Fact]
		public void When_different_fields_Should_not_replace()
		{
			var fake = new Fake<TestClass<int>>(5);
			
			var sut = fake.Rewrite(f => f.GetValue());
			sut.Replace((TestClass<double> t) => t.AnotherField).Return(7);
			
			sut.Execute().Should().Be(5);
		}

		private class TestClass<T>
		{
			public T Field;
			public T AnotherField;

			public TestClass(T value)
			{
				AnotherField = Field = value;
			}

			public T GetValue() => Field;
		}

		public class TestClass
		{
			public DateTime GetValueByArguments(DateTime dateTime, TimeZoneInfo zone)
			{
				Debug.WriteLine("Started");
				var value = TimeZoneInfo.ConvertTimeFromUtc(dateTime, zone);
				Debug.WriteLine("Finished");
				return value;
			}

			public int CodeBranch(int a, int b) => a + b;

			public int Sum(int a, int b)
			{
				if (a > 0)
				{
					return CodeBranch(a, b) + CodeBranch(a, b);
				}
				else
				{
					return CodeBranch(0, 0);
				}
			}

			public TestClass2 MutateTestClass2(TestClass2 cl, int value)
			{
				return Mutator(cl, value);
			}

			public TestClass2 Mutator(TestClass2 cl, int value)
			{
				cl.Value = value;
				return cl;
			}
		}

		public class TestClass2
		{
			public int Value { get; set; }
		}
	}
}
