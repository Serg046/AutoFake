using System;
using FluentAssertions;
using Xunit;

namespace AutoFake.IntegrationTests.InstanceTests
{
    public class CrossAssemblyTests
    {
        [Fact]
        public void ClassCtorTest()
        {
            var fake = new Fake<TestClass>();
            var sut = fake.Rewrite(f => f.GetClassCtorResult());
            sut.Replace(() => DateTime.Now).Return(DateTime.Now);

            Assert.Equal(7, sut.Execute().Prop);
        }

        [Fact]
        public void StructCtorTest()
        {
            var fake = new Fake<TestClass>();
            var sut = fake.Rewrite(f => f.GetStructCtorResult());
            sut.Replace(() => DateTime.Now).Return(DateTime.Now);

            Assert.Equal(7, sut.Execute().Prop);
        }

        [Fact]
        public void ClassCtorWithArgsTest()
        {
            var fake = new Fake<TestClass>();
            var sut = fake.Rewrite(f => f.GetClassCtorWithArgsResult());
            sut.Replace(() => DateTime.Now).Return(DateTime.Now);

            Assert.Equal(7, sut.Execute().Prop);
        }

        [Fact]
        public void ClassFieldTest()
        {
            var fake = new Fake<TestClass>();
            var sut = fake.Rewrite(f => f.GetClassField());
            sut.Replace(() => DateTime.Now).Return(DateTime.Now);

            Assert.Equal(7, sut.Execute().Prop);
        }

        [Fact]
        public void StructFieldTest()
        {
            var fake = new Fake<TestClass>();
            var sut = fake.Rewrite(f => f.GetStructField());
            sut.Replace(() => DateTime.Now).Return(DateTime.Now);

            Assert.Equal(7, sut.Execute().Prop);
        }

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
        public void BaseClassContractTest()
        {
	        var fake = new Fake<InheritedTestClass>();
	        var sut = fake.Rewrite(f => f.CallMethodThroughBaseClass(new HelperClass()));

	        sut.Execute().Should().Be(5);
        }

        [Fact(Skip = "TODO")]
        public void InterfaceContractThroughAnotherTypeTest()
        {
	        var fake = new Fake<InheritedTestClassWithInterface>();

	        fake.Rewrite(f => f.CallMethodUsingAnotherType(new HelperClass()))
		        .Execute().Should().Be(10);
        }

        public interface IHelper : IHelper2 { }
        public interface IHelper2 : IHelper3 { }
        public interface IHelper3
        {
	        int GetFive();
        }

        public class HelperClassBase : IHelper
        {
            public int GetFive() => 5;
        }

        public class HelperClass : HelperClassBase
        {
            public HelperClass() { }
            public HelperClass(int arg1, string arg2) { }

            public int Prop { get; set; }
        }
        
        public struct HelperStruct : IHelper
        {
            public int Prop { get; set; }

            public int GetFive() => 5;
        }

        private class TestClass
        {
            private readonly HelperClass _helperClassField = new HelperClass();
            private HelperStruct _helperStructField;

            public HelperClass GetClassCtorResult() => new HelperClass{Prop = 7};
            public HelperStruct GetStructCtorResult() => new HelperStruct { Prop = 7};
            public HelperClass GetClassCtorWithArgsResult() => new HelperClass(1, "2") {Prop = 7};

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

            public virtual int CallMethodThroughInterface(IHelper helper)
            {
	            return helper.GetFive();
            }

            public virtual int CallMethodThroughBaseClass(HelperClassBase helper)
            {
	            return helper.GetFive();
            }
        }

        private class TestClassWithInterfaceCtor
        {
	        private readonly IHelper _helper;

            public TestClassWithInterfaceCtor() {}
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

	        public TestClassWithBaseClassCtor() {}
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
