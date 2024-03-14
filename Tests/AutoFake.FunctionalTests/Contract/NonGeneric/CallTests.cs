using System;
using AutoFake.Abstractions;
using DryIoc;
using FluentAssertions;
using Mono.Cecil;
using Xunit;

namespace AutoFake.FunctionalTests.Contract.NonGeneric
{
	public class CallTests
	{
		[ExcludedTheory]
		[InlineData(typeof(HelperClass))]
		[InlineData(typeof(HelperStruct))]
		public void When_method_call_through_interface_Should_succeed(Type type)
		{
			var helper = Activator.CreateInstance(type) as IHelper;

			var fake = new Fake<TestClass>();
			var sut = fake.Rewrite(f => f.CallMethodThroughInterface(helper));

			sut.Execute().Should().Be(5);
		}

		[ExcludedFact]
		public void When_method_call_through_base_class_Should_succeed()
		{
			var fake = new Fake<TestClass>();
			var sut = fake.Rewrite(f => f.CallMethodThroughBaseClass(new HelperClass()));

			sut.Execute().Should().Be(5);
		}

		[ExcludedTheory]
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

		[ExcludedFact]
		public void When_method_call_through_base_class_from_field_Should_succeed()
		{
			var fake = new Fake<TestClassWithFields>();
			var sut = fake.Rewrite(f => f.CallMethodThroughBaseClass());
			sut.Replace(f => f.HelperBaseClass).Return(new HelperClass());

			sut.Execute().Should().Be(5);
		}

		[ExcludedTheory]
		[InlineData(typeof(HelperClass))]
		[InlineData(typeof(HelperStruct))]
		public void When_interface_contract_Should_succeed(Type type)
		{
			var helper = Activator.CreateInstance(type) as IHelper;

			var fake = new Fake<InheritedTestClassWithInterface>();
			var sut = fake.Rewrite(f => f.CallMethodThroughInterface(helper));

			sut.Execute().Should().Be(5);
		}

		[ExcludedFact]
		public void When_interface_contract_through_impl_class_Should_succeed()
		{
			var helper = new HelperClass();

			var fake = new Fake<InheritedTestClassWithInterface>();
			var sut = fake.Rewrite(f => f.CallMethodThroughImplClass(helper));

			sut.Execute().Should().Be(7);
		}

		[ExcludedFact]
		public void When_interface_contract_through_impl_class_interface_Should_succeed()
		{
			var helper = new HelperClass();

			var fake = new Fake<InheritedTestClassWithInterface>();
			var sut = fake.Rewrite(f => f.CallMethodThroughImplClassInterface(helper));

			sut.Execute().Should().Be(7);
		}

		[ExcludedFact]
		public void When_base_class_contract_Should_succeed()
		{
			var fake = new Fake<InheritedTestClass>();
			var sut = fake.Rewrite(f => f.CallMethodThroughBaseClass(new HelperClass()));

			sut.Execute().Should().Be(5);
		}

		[ExcludedFact]
		public void When_interface_contract_through_another_type_Should_succeed()
		{
			var fake = new Fake<InheritedTestClassWithInterface>();

			var sut = fake.Rewrite(f => f.CallMethodUsingAnotherType(new HelperClass()));

			sut.Execute().Should().Be(10);
		}

		[ExcludedFact]
		public void When_class_creation_Should_succeed()
		{
			var fake = new Fake<TestClass>();

			var sut = fake.Rewrite(f => f.CreateHelperClass());

			sut.Execute().Should().BeOfType<HelperClass>();
		}

		[ExcludedFact]
		public void When_struct_creation_Should_succeed()
		{
			var fake = new Fake<TestClass>();

			var sut = fake.Rewrite(f => f.CreateHelperStruct());

			sut.Execute().Should().BeOfType<HelperStruct>();
		}

		[ExcludedFact]
		public void When_boxing_Should_succeed()
		{
			var fake = new Fake<TestClass>();

			var sut = fake.Rewrite(f => f.ReturnBoxedHelperStruct(new HelperStruct()));

			sut.Execute().Should().BeOfType<HelperStruct>();
		}

		[ExcludedFact]
		public void When_unboxing_Should_succeed()
		{
			var fake = new Fake<TestClass>();

			var sut = fake.Rewrite(f => f.ReturnUnboxedHelperStruct(new HelperStruct()));

			sut.Execute().Should().BeOfType<HelperStruct>();
		}

		[ExcludedFact]
		public void When_replace_mock_args_Should_succeed()
		{
			var fake = new Fake<TestClass>();

			var sut = fake.Rewrite(f => f.CallMethodThroughInterface());
			sut.Replace(f => f.CallMethodThroughInterface(Arg.Is<IHelper>(IsHelperClass))).Return(88);

			sut.Execute().Should().Be(88);
		}

		[ExcludedFact]
		public void When_explicit_interface_member_Should_succeed()
		{
			var fake = new Fake<TestClassWithExplicitInterface>();

			var sut = fake.Rewrite(f => f.GetFive(new HelperClass()));
			sut.Replace((IHelper helper) => helper.GetFive()).Return(7);

			sut.Execute().Should().Be(7);
		}

		private bool IsHelperClass(IHelper helper) => helper is HelperClass { Prop: 4 };

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
