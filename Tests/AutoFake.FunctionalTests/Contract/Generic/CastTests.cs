using FluentAssertions;
using System;
using Xunit;

namespace AutoFake.FunctionalTests.Contract.Generic
{
	public class CastTests
	{
		[Fact]
		public void When_generic_class_cast_Should_succeed()
		{
			var fake = new Fake<TestClass>();
			var helper = new HelperClass<int>();

			var sut = fake.Rewrite(f => f.CastToGenericHelperClass<int>(helper));

			sut.Execute().Should().Be(helper);
		}

		[Fact]
		public void When_generic_struct_cast_Should_succeed()
		{
			var fake = new Fake<TestClass>();
			var helper = new HelperStruct<int>();

			var sut = fake.Rewrite(f => f.CastToGenericHelperStruct<int>(helper));

			sut.Execute().Should().Be(helper);
		}

		[Theory]
		[InlineData(typeof(HelperClass))]
		[InlineData(typeof(HelperStruct))]
		public void When_generic_interface_cast_Should_succeed(Type type)
		{
			var fake = new Fake<TestClass>();
			var helper = Activator.CreateInstance(type) as IHelper<int>;

			var sut = fake.Rewrite(f => f.CastToGenericHelperInterface<int>(helper));

			sut.Execute().Should().Be(helper);
		}

		[Fact]
		public void When_no_contract_Should_succeed()
		{
			var fake = new Fake<TestClass>();

			var sut = fake.Rewrite(f => f.CreateAndCastToObject());
			sut.Import<HelperStruct<int>>();

			sut.Execute().Should().BeOfType<HelperStruct<int>>()
				.Subject.Value.Should().Be(5);
		}

		[Fact]
		public void When_no_contract_with_generic_in_generic_Should_succeed()
		{
			var fake = new Fake<TestClass>();

			var sut = fake.Rewrite(f => f.CreateGenericAndCastToObject());
			sut.Import<HelperStruct<HelperClass>>();

			sut.Execute().Should().BeOfType<HelperStruct<HelperClass>>()
				.Subject.Value.Prop.Should().Be(5);
		}

		private class TestClass
		{
			public HelperClass<T> CastToGenericHelperClass<T>(object helper)
			{
				return (HelperClass<T>)helper;
			}

			public HelperStruct<T> CastToGenericHelperStruct<T>(object helper)
			{
				return (HelperStruct<T>)helper;
			}

			public IHelper<T> CastToGenericHelperInterface<T>(object helper)
			{
				return (IHelper<T>)helper;
			}

			public object CreateAndCastToObject()
			{
				return new HelperStruct<int>(5);
			}

			public object CreateGenericAndCastToObject()
			{
				return new HelperStruct<HelperClass>(new HelperClass { Prop = 5 });
			}
		}

		public interface IHelper
		{
			int GetFive();
		}

		public class HelperClass : IHelper
		{
			public int Prop { get; set; }
			public int GetFive() => 5;
		}

		public struct HelperStruct : IHelper
		{
			public int Prop { get; set; }
			public int GetFive() => 5;
		}

		public interface IHelper<T>
		{
			T GetValue(T value);
		}

		public class HelperClass<T> : IHelper<T>
		{
			public T GetValue(T value) => value;
		}

		public struct HelperStruct<T> : IHelper<T>
		{
			public HelperStruct(T value) => Value = value;

			public T Value { get; }

			public T GetValue(T value) => value;
		}
	}
}
