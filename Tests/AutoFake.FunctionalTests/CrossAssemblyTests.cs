using System;
using System.Collections.Generic;
using AnotherSut;
using AutoFake.Exceptions;
using FluentAssertions;
using Sut;
using Xunit;

namespace AutoFake.FunctionalTests
{
	public class CrossAssemblyTests
	{
		[Fact]
		public void When_class_ctor_Should_succeed()
		{
			var fake = new Fake<TestClass>();
			var sut = fake.Rewrite(f => f.GetClassCtorResult());
			sut.Replace(() => DateTime.Now).Return(DateTime.Now);

			Assert.Equal(7, sut.Execute().Prop);
		}

		[Fact]
		public void When_struct_ctor_Should_succeed()
		{
			var fake = new Fake<TestClass>();
			var sut = fake.Rewrite(f => f.GetStructCtorResult());
			sut.Replace(() => DateTime.Now).Return(DateTime.Now);

			Assert.Equal(7, sut.Execute().Prop);
		}

		[Fact]
		public void When_class_ctor_with_args_Should_succeed()
		{
			var fake = new Fake<TestClass>();
			var sut = fake.Rewrite(f => f.GetClassCtorWithArgsResult());
			sut.Replace(() => DateTime.Now).Return(DateTime.Now);

			Assert.Equal(7, sut.Execute().Prop);
		}

		[Fact]
		public void When_class_field_Should_be_returned()
		{
			var fake = new Fake<TestClass>();
			var sut = fake.Rewrite(f => f.GetClassField());
			sut.Replace(() => DateTime.Now).Return(DateTime.Now);

			Assert.Equal(7, sut.Execute().Prop);
		}

		[Fact]
		public void When_struct_field_Should_be_returned()
		{
			var fake = new Fake<TestClass>();
			var sut = fake.Rewrite(f => f.GetStructField());
			sut.Replace(() => DateTime.Now).Return(DateTime.Now);

			Assert.Equal(7, sut.Execute().Prop);
		}

		[Fact]
		public void When_mock_another_assembly_member_Should_be_replaced()
		{
			var fake = new Fake<TestClass>();
			fake.Options.AddReference(typeof(SystemUnderTest));
			fake.Options.AddReference(typeof(AnotherSystemUnderTest));

			var sut = fake.Rewrite(f => f.GetDateFromAnotherAssembly());
			sut.Replace(() => DateTime.Now).Return(DateTime.MaxValue);

			sut.Execute().Should().Be(DateTime.MaxValue);
		}

		[Theory]
		[InlineData(0, false)]
		[InlineData(1, true)]
		[InlineData(2, false)]
		public void When_mock_another_assembly_member_Should_be_verified(byte expectedCalls, bool success)
		{
			var fake = new Fake<TestClass>();
			fake.Options.AddReference(typeof(SystemUnderTest));
			fake.Options.AddReference(typeof(AnotherSystemUnderTest));

			var sut = fake.Rewrite(f => f.GetDateFromAnotherAssembly());
			sut.Verify(() => DateTime.Now).ExpectedCalls(expectedCalls);

			Action act = () => sut.Execute();

			if (success)
			{
				act.Should().NotThrow();
			}
			else
			{
				act.Should().Throw<ExpectedCallsException>();
			}
		}

		[Fact]
		public void When_mock_another_assembly_member_Should_be_inserted()
		{
			var list = new List<int>();
			var fake = new Fake<TestClass>();
			fake.Options.AddReference(typeof(SystemUnderTest));
			fake.Options.AddReference(typeof(AnotherSystemUnderTest));
			var sut = fake.Rewrite(f => f.GetDateFromAnotherAssembly());
			sut.Prepend(() => list.Add(0));
			sut.Prepend(() => list.Add(1)).Before(() => DateTime.Now);
			sut.Append(() => list.Add(2)).After(() => DateTime.Now);
			sut.Append(() => list.Add(3));

			sut.Execute();

			list.Should().ContainInOrder(0, 1, 2, 3);
		}

		public class HelperClass
		{
			public HelperClass() { }
			public HelperClass(int arg1, string arg2) { }

			public int Prop { get; set; }
		}

		public struct HelperStruct
		{
			public int Prop { get; set; }
		}

		private class TestClass
		{
			private readonly HelperClass _helperClassField = new HelperClass();
			private HelperStruct _helperStructField;

			public HelperClass GetClassCtorResult() => new HelperClass { Prop = 7 };
			public HelperStruct GetStructCtorResult() => new HelperStruct { Prop = 7 };
			public HelperClass GetClassCtorWithArgsResult() => new HelperClass(1, "2") { Prop = 7 };

			public HelperClass GetClassField()
			{
				_helperClassField.Prop = 7;
				return _helperClassField;
			}

			public HelperStruct GetStructField()
			{
				_helperStructField.Prop = 7;
				return _helperStructField;
			}

			public DateTime GetDateFromAnotherAssembly()
			{
				var sut = new SystemUnderTest();
				return sut.GetCurrentDateFromAnotherSut();
			}
		}
	}
}
