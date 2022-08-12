using FluentAssertions;
using System;
using Xunit;

namespace AutoFake.FunctionalTests
{
	public class TestMethodContractCastTests
	{
		[Fact]
        public void When_class_cast_Should_succeed()
        {
            var fake = new Fake<TestClass>();
            var helper = new HelperClass();

            var sut = fake.Rewrite(f => f.CastToHelperClass(helper));

            sut.Execute().Should().Be(helper);
        }

        [Fact]
        public void When_struct_cast_Should_succeed()
        {
            var fake = new Fake<TestClass>();
            var helper = new HelperStruct();

            var sut = fake.Rewrite(f => f.CastToHelperStruct(helper));

            sut.Execute().Should().Be(helper);
        }

        [Theory]
        [InlineData(typeof(HelperClass))]
        [InlineData(typeof(HelperStruct))]
        public void When_interface_cast_Should_succeed(Type type)
        {
            var fake = new Fake<TestClass>();
            var helper = Activator.CreateInstance(type) as IHelper;

            var sut = fake.Rewrite(f => f.CastToHelperInterface(helper));

            sut.Execute().Should().Be(helper);
        }

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

        private class TestClass
		{
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
        }

        public interface IHelper
        {
            int GetFive();
        }

        public class HelperClass : IHelper
        {
            public int GetFive() => 5;
        }

        public struct HelperStruct : IHelper
        {
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
            public T GetValue(T value) => value;
        }
    }
}
