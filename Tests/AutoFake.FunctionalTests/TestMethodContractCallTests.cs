using System;
using FluentAssertions;
using Xunit;

namespace AutoFake.FunctionalTests
{
	public class TestMethodContractCallTests
	{
		[Theory]
		[InlineData(typeof(HelperClass))]
		[InlineData(typeof(HelperStruct))]
		public void When_method_call_through_interface_Should_succeed(Type type)
		{
			var helper = Activator.CreateInstance(type) as IHelper;

			var fake = new Fake<TestClass>();
			var sut = fake.Rewrite(f => f.CallMethodThroughInterface(helper));

			sut.Execute().Should().Be(5);
		}

		[Fact]
		public void When_method_call_through_base_class_Should_succeed()
		{
			var fake = new Fake<TestClass>();
			var sut = fake.Rewrite(f => f.CallMethodThroughBaseClass(new HelperClass()));

			sut.Execute().Should().Be(5);
		}

		[Theory]
		[InlineData(typeof(HelperClass))]
		[InlineData(typeof(HelperStruct))]
		public void When_method_call_through_interface_from_ctor_Should_succeed(Type type)
		{
			var helper = Activator.CreateInstance(type) as IHelper;

			var fake = new Fake<TestClassWithInterfaceCtor>(helper);
			var sut = fake.Rewrite(f => f.CallMethodThroughInterface());

			sut.Execute().Should().Be(5);
		}

		[Fact]
		public void When_method_call_through_base_class_from_ctor_Should_succeed()
		{
			var fake = new Fake<TestClassWithBaseClassCtor>(new HelperClass());
			var sut = fake.Rewrite(f => f.CallMethodThroughBaseClass());

			sut.Execute().Should().Be(5);
		}

		[Theory]
		[InlineData(typeof(HelperClass))]
		[InlineData(typeof(HelperStruct))]
		public void When_method_call_through_interface_from_field_Should_succeed(Type type)
		{
			var helper = Activator.CreateInstance(type) as IHelper;

			var fake = new Fake<TestClassWithFields>();
			var sut = fake.Rewrite(f => f.CallMethodThroughInterface());
			sut.Replace(f => f.HelperInterface).Return(helper);

			sut.Execute().Should().Be(5);
		}

		[Fact]
		public void When_method_call_through_base_class_from_field_Should_succeed()
		{
			var fake = new Fake<TestClassWithFields>();
			var sut = fake.Rewrite(f => f.CallMethodThroughBaseClass());
			sut.Replace(f => f.HelperBaseClass).Return(new HelperClass());

			sut.Execute().Should().Be(5);
		}

		[Theory]
		[InlineData(typeof(HelperClass))]
		[InlineData(typeof(HelperStruct))]
		public void When_interface_contract_Should_succeed(Type type)
		{
			var helper = Activator.CreateInstance(type) as IHelper;

			var fake = new Fake<InheritedTestClassWithInterface>();
			var sut = fake.Rewrite(f => f.CallMethodThroughInterface(helper));

			sut.Execute().Should().Be(5);
		}

		[Fact]
		public void When_interface_contract_through_impl_class_Should_succeed()
		{
			var helper = new HelperClass();

			var fake = new Fake<InheritedTestClassWithInterface>();
			var sut = fake.Rewrite(f => f.CallMethodThroughImplClass(helper));

			sut.Execute().Should().Be(7);
		}

		[Fact]
		public void When_interface_contract_through_impl_class_interface_Should_succeed()
		{
			var helper = new HelperClass();

			var fake = new Fake<InheritedTestClassWithInterface>();
			var sut = fake.Rewrite(f => f.CallMethodThroughImplClassInterface(helper));

			sut.Execute().Should().Be(7);
		}

		[Fact]
		public void When_base_class_contract_Should_succeed()
		{
			var fake = new Fake<InheritedTestClass>();
			var sut = fake.Rewrite(f => f.CallMethodThroughBaseClass(new HelperClass()));

			sut.Execute().Should().Be(5);
		}

		[Fact]
		public void When_interface_contract_through_another_type_Should_succeed()
		{
			var fake = new Fake<InheritedTestClassWithInterface>();

			var sut = fake.Rewrite(f => f.CallMethodUsingAnotherType(new HelperClass()));

			sut.Execute().Should().Be(10);
		}

		[Fact]
		public void When_class_creation_Should_succeed()
		{
			var fake = new Fake<TestClass>();

			var sut = fake.Rewrite(f => f.CreateHelperClass());

			sut.Execute().Should().BeOfType<HelperClass>();
		}

		[Fact]
		public void When_struct_creation_Should_succeed()
		{
			var fake = new Fake<TestClass>();

			var sut = fake.Rewrite(f => f.CreateHelperStruct());

			sut.Execute().Should().BeOfType<HelperStruct>();
		}

		[Fact]
		public void When_boxing_Should_succeed()
		{
			var fake = new Fake<TestClass>();

			var sut = fake.Rewrite(f => f.ReturnBoxedHelperStruct(new HelperStruct()));

			sut.Execute().Should().BeOfType<HelperStruct>();
		}

		[Fact]
		public void When_unboxing_Should_succeed()
		{
			var fake = new Fake<TestClass>();

			var sut = fake.Rewrite(f => f.ReturnUnboxedHelperStruct(new HelperStruct()));

			sut.Execute().Should().BeOfType<HelperStruct>();
		}

		[Fact]
		public void When_replace_mock_args_Should_succeed()
		{
			var fake = new Fake<TestClass>();

			var sut = fake.Rewrite(f => f.CallMethodThroughInterface());
			sut.Replace(f => f.CallMethodThroughInterface(Arg.Is<IHelper>(IsHelperClass))).Return(88);

			sut.Execute().Should().Be(88);
		}

		[Theory]
		[InlineData(typeof(GenericClassHelper<int>))]
		[InlineData(typeof(GenericStructHelper<int>))]
		public void When_method_call_through_generic_interface_Should_succeed(Type type)
		{
			var helper = Activator.CreateInstance(type, 5) as IGenericHelper<int>;

			var fake = new Fake<TestClass>();
			var sut = fake.Rewrite(f => f.CallMethodThroughGenericInterface(helper));

			sut.Execute().Should().Be(5);
		}

		[Theory]
		[InlineData(typeof(GenericClassHelper<int>))]
		[InlineData(typeof(GenericStructHelper<int>))]
		public void When_method_call_through_inferred_generic_interface_Should_succeed(Type type)
		{
			var helper = Activator.CreateInstance(type, 5) as IGenericHelper<int>;

			var fake = new Fake<GenericTestClass<int>>();
			var sut = fake.Rewrite(f => f.CallMethodThroughGenericInterface(helper));

			sut.Execute().Should().Be(5);
		}

		[Theory]
		[InlineData(typeof(GenericClassHelper<int>))]
		[InlineData(typeof(GenericStructHelper<int>))]
		public void When_generic_method_call_through_generic_interface_Should_succeed(Type type)
		{
			var helper = Activator.CreateInstance(type, 5) as IGenericHelper<int>;

			var fake = new Fake<TestClass>();
			var sut = fake.Rewrite(f => f.CallGenericMethodThroughGenericInterface(helper));

			sut.Execute().Prop.Should().Be(5);
		}

		[Theory]
		[InlineData(typeof(GenericClassHelper<TestClass>))]
		[InlineData(typeof(GenericStructHelper<TestClass>))]
		public void When_generic_method_call_through_internal_generic_interface_Should_succeed(Type type)
		{
			var helper = Activator.CreateInstance(type, new TestClass { Prop = 5 }) as IGenericHelper<TestClass>;

			var fake = new Fake<TestClass>();
			var sut = fake.Rewrite(f => f.CallGenericMethodThroughGenericInterface(helper));

			sut.Execute().Should().Be(helper.Value);
		}

		[Fact]
		public void When_generic_type_inside_Should_succeed()
		{
			var fake = new Fake<TestClass>();
			var sut = fake.Rewrite(f => f.CallGenericMethodThroughGenericInterface<TestClass>());

			sut.Execute().Should().BeOfType<TestClass>();
			sut.Execute().Prop.Should().Be(5);
		}

		[Fact]
		public void When_generic_class_creation_Should_succeed()
		{
			var fake = new Fake<TestClass>();

			var sut = fake.Rewrite(f => f.CreateGenericHelperClass());

			sut.Execute().Should().BeOfType<GenericClassHelper<int>>();
		}

		[Fact]
		public void When_generic_struct_creation_Should_succeed()
		{
			var fake = new Fake<TestClass>();

			var sut = fake.Rewrite(f => f.CreateGenericHelperStruct());

			sut.Execute().Should().BeOfType<GenericStructHelper<int>>();
		}

		[Fact]
		public void When_explicit_interface_member_Should_succeed()
		{
			var fake = new Fake<TestClassWithExplicitInterface>();

			var sut = fake.Rewrite(f => f.GetFive(new HelperClass()));
			sut.Replace((IHelper helper) => helper.GetFive()).Return(7);

			sut.Execute().Should().Be(7);
		}

		private bool IsHelperClass(IHelper helper) => helper is HelperClass { Prop: 4 };

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

		public interface IHelper : IHelper2 { }
		public interface IHelper2 : IHelper3 { }
		public interface IHelper3
		{
			int GetFive();
		}

		public interface IHelperSeven
		{
			int GetSeven();
		}

		public class HelperClassBase : IHelper
		{
			public int GetFive() => 5;
		}

		public class HelperClass : HelperClassBase, IHelperSeven
		{
			public HelperClass() { }
			public HelperClass(int arg1, string arg2) { }

			public int Prop { get; set; }

			public int GetSeven() => 7;
		}

		public struct HelperStruct : IHelper
		{
			public int Prop { get; set; }

			public int GetFive() => 5;
		}

		private class GenericTestClass<T>
		{
			public double CallMethodThroughGenericInterface(IGenericHelper<T> helper)
			{
				return helper.GetValue(v => 5);
			}
		}

		public class TestClass
		{
			public int Prop { get; set; }

			public int CallMethodThroughInterface()
			{
				return CallMethodThroughInterface(new HelperClass { Prop = 4 });
			}

			public virtual int CallMethodThroughInterface(IHelper helper)
			{
				return helper.GetFive();
			}

			public double CallMethodThroughGenericInterface(IGenericHelper<int> helper)
			{
				return helper.GetValue(v => v);
			}

			public TestClass CallGenericMethodThroughGenericInterface(IGenericHelper<int> helper)
			{
				return helper.GetValueGeneric<TestClass>(v => new TestClass { Prop = v });
			}

			public T CallGenericMethodThroughGenericInterface<T>(IGenericHelper<T> helper)
			{
				return helper.Value;
			}

			public TestClass CallGenericMethodThroughGenericInterface<T>()
			{
				return new GenericClassHelper<TestClass>(new TestClass { Prop = 5 }).Value;
			}

			public int CallMethodThroughImplClass(HelperClassBase helper)
			{
				return ((HelperClass)helper).GetSeven();
			}

			public int CallMethodThroughImplClassInterface(HelperClassBase helper)
			{
				return ((IHelperSeven)helper).GetSeven();
			}

			public virtual int CallMethodThroughBaseClass(HelperClassBase helper)
			{
				return helper.GetFive();
			}

			public IHelper CreateHelperClass()
			{
				return new HelperClass();
			}

			public IHelper CreateHelperStruct()
			{
				return new HelperStruct();
			}

			public IGenericHelper<int> CreateGenericHelperClass()
			{
				return new GenericClassHelper<int>(5);
			}

			public IGenericHelper<int> CreateGenericHelperStruct()
			{
				return new GenericStructHelper<int>(5);
			}

			public object ReturnBoxedHelperStruct(HelperStruct helperStruct)
			{
				return helperStruct;
			}

			public HelperStruct ReturnUnboxedHelperStruct(object helperStruct)
			{
				return (HelperStruct)helperStruct;
			}
		}

		private class TestClassWithInterfaceCtor
		{
			private readonly IHelper _helper;

			public TestClassWithInterfaceCtor() { }
			public TestClassWithInterfaceCtor(IHelper helper)
			{
				_helper = helper;
			}

			public int CallMethodThroughInterface()
			{
				return _helper.GetFive();
			}
		}

		private class TestClassWithBaseClassCtor
		{
			private readonly HelperClassBase _helper;

			public TestClassWithBaseClassCtor() { }
			public TestClassWithBaseClassCtor(HelperClassBase helper)
			{
				_helper = helper;
			}

			public int CallMethodThroughBaseClass()
			{
				return _helper.GetFive();
			}
		}

		private class TestClassWithFields
		{
			public IHelper HelperInterface;
			public HelperClassBase HelperBaseClass;

			public int CallMethodThroughInterface()
			{
				return HelperInterface.GetFive();
			}

			public int CallMethodThroughBaseClass()
			{
				return HelperBaseClass.GetFive();
			}
		}

		private interface IInheritedTestClassWithInterface
		{
			int CallMethodThroughInterface(IHelper helper);
			int CallMethodThroughBaseClass(HelperClassBase helper);
		}

		private class InheritedTestClass : TestClass
		{
			public override int CallMethodThroughInterface(IHelper helper)
			{
				Console.WriteLine("Some action");
				return base.CallMethodThroughInterface(helper);
			}

			public override int CallMethodThroughBaseClass(HelperClassBase helper)
			{
				Console.WriteLine("Some action");
				return base.CallMethodThroughBaseClass(helper);
			}
		}

		private class InheritedTestClassWithInterface : TestClass, IInheritedTestClassWithInterface
		{
			public int CallMethodUsingAnotherType(IHelper helper)
			{
				return CallMethodUsingAnotherType(this, helper);
			}

			private int CallMethodUsingAnotherType(IInheritedTestClassWithInterface testClass, IHelper helper)
			{
				return testClass.CallMethodThroughInterface(helper) +
					   new AnotherInheritedTestClassWithInterface().CallMethodThroughInterface(helper);
			}
		}

		private class AnotherInheritedTestClassWithInterface : IInheritedTestClassWithInterface
		{
			public int CallMethodThroughInterface(IHelper helper)
			{
				return helper.GetFive();
			}

			public int CallMethodThroughBaseClass(HelperClassBase helper)
			{
				return helper.GetFive();
			}
		}

		public interface ITestClassWithExplicitInterface
		{
			int GetFive(IHelper helper);
		}

		public class TestClassWithExplicitInterface : ITestClassWithExplicitInterface
		{
			int ITestClassWithExplicitInterface.GetFive(IHelper helper) => GetFive(helper);
			public int GetFive(IHelper helper) => helper.GetFive();
		}
	}
}
