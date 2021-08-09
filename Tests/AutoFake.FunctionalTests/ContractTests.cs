using System;
using FluentAssertions;
using Xunit;

namespace AutoFake.FunctionalTests
{
    public class ContractTests
    {
        [Theory]
        [InlineData(typeof(HelperClass))]
        [InlineData(typeof(HelperStruct))]
        public void MethodCallThroughInterfaceTest(Type type)
        {
            var helper = Activator.CreateInstance(type) as IHelper;

            var fake = new Fake<TestClass>();
            var sut = fake.Rewrite(f => f.CallMethodThroughInterface(helper));

            sut.Execute().Should().Be(5);
        }

        [Fact]
        public void MethodCallThroughBaseClassTest()
        {
            var fake = new Fake<TestClass>();
            var sut = fake.Rewrite(f => f.CallMethodThroughBaseClass(new HelperClass()));

            sut.Execute().Should().Be(5);
        }

        [Theory]
        [InlineData(typeof(HelperClass))]
        [InlineData(typeof(HelperStruct))]
        public void MethodCallThroughInterfaceFromCtorTest(Type type)
        {
            var helper = Activator.CreateInstance(type) as IHelper;

            var fake = new Fake<TestClassWithInterfaceCtor>(helper);
            var sut = fake.Rewrite(f => f.CallMethodThroughInterface());

            sut.Execute().Should().Be(5);
        }

        [Fact]
        public void MethodCallThroughBaseClassFromCtorTest()
        {
            var fake = new Fake<TestClassWithBaseClassCtor>(new HelperClass());
            var sut = fake.Rewrite(f => f.CallMethodThroughBaseClass());

            sut.Execute().Should().Be(5);
        }

        [Theory]
        [InlineData(typeof(HelperClass))]
        [InlineData(typeof(HelperStruct))]
        public void MethodCallThroughInterfaceFromFieldTest(Type type)
        {
            var helper = Activator.CreateInstance(type) as IHelper;

            var fake = new Fake<TestClassWithFields>();
            var sut = fake.Rewrite(f => f.CallMethodThroughInterface());
            sut.Replace(f => f.HelperInterface).Return(helper);

            sut.Execute().Should().Be(5);
        }

        [Fact]
        public void MethodCallThroughBaseClassFromFieldTest()
        {
            var fake = new Fake<TestClassWithFields>();
            var sut = fake.Rewrite(f => f.CallMethodThroughBaseClass());
            sut.Replace(f => f.HelperBaseClass).Return(new HelperClass());

            sut.Execute().Should().Be(5);
        }

        [Theory]
        [InlineData(typeof(HelperClass))]
        [InlineData(typeof(HelperStruct))]
        public void InterfaceContractTest(Type type)
        {
            var helper = Activator.CreateInstance(type) as IHelper;

            var fake = new Fake<InheritedTestClassWithInterface>();
            var sut = fake.Rewrite(f => f.CallMethodThroughInterface(helper));

            sut.Execute().Should().Be(5);
        }

        [Fact]
        public void InterfaceContractThroughImplClassTest()
        {
            var helper = new HelperClass();

            var fake = new Fake<InheritedTestClassWithInterface>();
            var sut = fake.Rewrite(f => f.CallMethodThroughImplClass(helper));

            sut.Execute().Should().Be(7);
        }

        [Fact]
        public void InterfaceContractThroughImplClassInterfaceTest()
        {
            var helper = new HelperClass();

            var fake = new Fake<InheritedTestClassWithInterface>();
            var sut = fake.Rewrite(f => f.CallMethodThroughImplClassInterface(helper));

            sut.Execute().Should().Be(7);
        }

        [Fact]
        public void BaseClassContractTest()
        {
            var fake = new Fake<InheritedTestClass>();
            var sut = fake.Rewrite(f => f.CallMethodThroughBaseClass(new HelperClass()));

            sut.Execute().Should().Be(5);
        }

        [Fact(Skip = "Issue #144")]
        public void InterfaceContractThroughAnotherTypeTest()
        {
            var fake = new Fake<InheritedTestClassWithInterface>();

            //fake.RewriteContract(f => f.CallMethodThroughInterface(new HelperClass()));
            //fake.RewriteContract((AnotherInheritedTestClassWithInterface f) => f.CallMethodThroughInterface(new HelperClass()));
            var sut = fake.Rewrite(f => f.CallMethodUsingAnotherType(new HelperClass()));

            sut.Execute().Should().Be(10);
        }

        [Fact]
        public void ClassCastTest()
        {
            var fake = new Fake<TestClass>();
            var helper = new HelperClass();

            var sut = fake.Rewrite(f => f.CastToHelperClass(helper));

            sut.Execute().Should().Be(helper);
        }

        [Fact]
        public void StructCastTest()
        {
            var fake = new Fake<TestClass>();
            var helper = new HelperStruct();

            var sut = fake.Rewrite(f => f.CastToHelperStruct(helper));

            sut.Execute().Should().Be(helper);
        }

        [Theory]
        [InlineData(typeof(HelperClass))]
        [InlineData(typeof(HelperStruct))]
        public void InterfaceCastTest(Type type)
        {
            var fake = new Fake<TestClass>();
            var helper = Activator.CreateInstance(type) as IHelper;

            var sut = fake.Rewrite(f => f.CastToHelperInterface(helper));

            sut.Execute().Should().Be(helper);
        }

        [Fact]
        public void ClassCreationTest()
        {
            var fake = new Fake<TestClass>();

            var sut = fake.Rewrite(f => f.CreateHelperClass());

            sut.Execute().Should().BeOfType<HelperClass>();
        }

        [Fact(Skip = "Some specifics regarding type casts")]
        public void StructCreationTest()
        {
            var fake = new Fake<TestClass>();

            var sut = fake.Rewrite(f => f.CreateHelperStruct());

            sut.Execute().Should().BeOfType<HelperStruct>();
        }

		[Fact(Skip = "Some specifics regarding type casts")]
		public void ReplaceMockArgsTest()
		{
			var fake = new Fake<TestClass>();

			var sut = fake.Rewrite(f => f.CallMethodThroughInterface());
			//sut.Replace(f => f.CallMethodThroughInterface(
			//	Arg.Is<IHelper>(a => a.GetType() == typeof(HelperClass)))).Return(88);
			sut.Replace(f => f.CallMethodThroughInterface(
			 Arg.Is<IHelper>(IsHelperClass))).Return(88);

			sut.Execute().Should().Be(88);
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

        private class TestClass
        {
	        public int CallMethodThroughInterface()
	        {
		        return CallMethodThroughInterface(new HelperClass {Prop = 4});
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

            public HelperClass CastToHelperClass(object helper)
            {
                return (HelperClass)helper;
            }

            public HelperStruct CastToHelperStruct(object helper)
            {
                return (HelperStruct)helper;
            }

            public IHelper CastToHelperInterface(object helper)
            {
                return (IHelper)helper;
            }

            public IHelper CreateHelperClass()
            {
                return new HelperClass();
            }

            public IHelper CreateHelperStruct()
            {
                return new HelperStruct();
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
	            return testClass.CallMethodThroughInterface(helper);// +
                       //new AnotherInheritedTestClassWithInterface().CallMethodThroughInterface(helper);
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
    }
}
