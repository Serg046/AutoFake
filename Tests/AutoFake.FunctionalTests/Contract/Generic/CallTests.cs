using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace AutoFake.FunctionalTests.Contract.Generic
{
	public class CallTests
	{
		[ExcludedTheory]
		[InlineData(typeof(GenericClassHelper<int>))]
		[InlineData(typeof(GenericStructHelper<int>))]
		public void When_method_call_through_generic_interface_Should_succeed(Type type)
		{
			var helper = Activator.CreateInstance(type, 5) as IGenericHelper<int>;

			var fake = new Fake<TestClass>();
			var sut = fake.Rewrite(f => f.CallMethodThroughGenericInterface(helper));

			sut.Execute().Should().Be(5);
		}

		[ExcludedTheory]
		[InlineData(typeof(GenericClassHelper<int>))]
		[InlineData(typeof(GenericStructHelper<int>))]
		public void When_method_call_through_inferred_generic_interface_Should_succeed(Type type)
		{
			var helper = Activator.CreateInstance(type, 5) as IGenericHelper<int>;

			var fake = new Fake<GenericTestClass<int>>();
			var sut = fake.Rewrite(f => f.CallMethodThroughGenericInterface(helper));

			sut.Execute().Should().Be(5);
		}

		[ExcludedTheory]
		[InlineData(typeof(GenericClassHelper<int>))]
		[InlineData(typeof(GenericStructHelper<int>))]
		public void When_generic_method_call_through_generic_interface_Should_succeed(Type type)
		{
			var helper = Activator.CreateInstance(type, 5) as IGenericHelper<int>;

			var fake = new Fake<TestClass>();
			var sut = fake.Rewrite(f => f.CallGenericMethodThroughGenericInterface(helper));

			sut.Execute().Prop.Should().Be(5);
		}

		[ExcludedTheory]
		[InlineData(typeof(GenericClassHelper<TestClass>))]
		[InlineData(typeof(GenericStructHelper<TestClass>))]
		public void When_generic_method_call_through_internal_generic_interface_Should_succeed(Type type)
		{
			var helper = Activator.CreateInstance(type, new TestClass { Prop = 5 }) as IGenericHelper<TestClass>;

			var fake = new Fake<TestClass>();
			var sut = fake.Rewrite(f => f.CallGenericMethodThroughGenericInterface(helper));

			sut.Execute().Should().Be(helper.Value);
		}

		[ExcludedFact]
		public void When_generic_type_inside_Should_succeed()
		{
			var fake = new Fake<TestClass>();
			var sut = fake.Rewrite(f => f.CallGenericMethodThroughGenericInterface<TestClass>());

			sut.Execute().Should().BeOfType<TestClass>();
			sut.Execute().Prop.Should().Be(5);
		}

		[ExcludedFact]
		public void When_generic_class_creation_Should_succeed()
		{
			var fake = new Fake<TestClass>();

			var sut = fake.Rewrite(f => f.CreateGenericHelperClass());

			sut.Execute().Should().BeOfType<GenericClassHelper<int>>();
		}

		[ExcludedFact]
		public void When_generic_struct_creation_Should_succeed()
		{
			var fake = new Fake<TestClass>();

			var sut = fake.Rewrite(f => f.CreateGenericHelperStruct());

			sut.Execute().Should().BeOfType<GenericStructHelper<int>>();
		}

		[Fact(Skip = "Issue #267")]
		public void When_generic_arg_Should_succeed()
		{
			var fake = new Fake<TestClass>();

			var testClasses = new List<TestClass>();
			var sut = fake.Rewrite(f => f.AddTestClass(testClasses));
			sut.Execute();

			testClasses.Should().HaveCount(1);
			testClasses.First().Prop.Should().Be(6);
		}

		public class TestClass
		{
			public int Prop { get; set; }

			public double CallMethodThroughGenericInterface(IGenericHelper<int> helper)
			{
				return helper.GetValue(v => v);
			}

			public TestClass CallGenericMethodThroughGenericInterface(IGenericHelper<int> helper)
			{
				return helper.GetValueGeneric(v => new TestClass { Prop = v });
			}

			public T CallGenericMethodThroughGenericInterface<T>(IGenericHelper<T> helper)
			{
				return helper.Value;
			}

			public TestClass CallGenericMethodThroughGenericInterface<T>()
			{
				return new GenericClassHelper<TestClass>(new TestClass { Prop = 5 }).Value;
			}

			public IGenericHelper<int> CreateGenericHelperClass()
			{
				return new GenericClassHelper<int>(5);
			}

			public IGenericHelper<int> CreateGenericHelperStruct()
			{
				return new GenericStructHelper<int>(5);
			}

			public void AddTestClass(ICollection<TestClass> items)
			{
				items.Add(new TestClass { Prop = 6 });
			}
		}

		private class GenericTestClass<T>
		{
			public double CallMethodThroughGenericInterface(IGenericHelper<T> helper)
			{
				return helper.GetValue(v => 5);
			}
		}

		public interface IGenericHelper<T>
		{
			T Value { get; }
			double GetValue(Func<T, double> functor);
			TReturn GetValueGeneric<TReturn>(Func<T, TReturn> functor);
		}

		public class GenericClassHelper<T> : IGenericHelper<T>
		{
			public GenericClassHelper(T value) => Value = value;
			public T Value { get; }
			public double GetValue(Func<T, double> functor) => functor(Value);
			public TReturn GetValueGeneric<TReturn>(Func<T, TReturn> functor) => functor(Value);
		}

		public struct GenericStructHelper<T> : IGenericHelper<T>
		{
			public GenericStructHelper(T value) => Value = value;
			public T Value { get; }
			public double GetValue(Func<T, double> functor) => functor(Value);
			public TReturn GetValueGeneric<TReturn>(Func<T, TReturn> functor) => functor(Value);
		}
	}
}
