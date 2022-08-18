using Xunit;

namespace AutoFake.FunctionalTests
{
    public class VirtualTests
    {
        [Fact]
        public void When_virtual_method_with_exact_name_configured_Should_execute_overriden_method()
        {
            var fake = new Fake<TestClass>();
            fake.Options.AllowedVirtualMembers.Add(m => m.Name == nameof(TestClass.GetNumber));

            var sut = fake.Rewrite(f => f.GetVirtualNumberByMethod());
            sut.Replace(() => NumberFactory.Get()).Return(2);

            Assert.Equal(2, sut.Execute());
        }

        [Fact]
        public void When_virtual_method_with_everything_configured_Should_execute_overriden_method()
        {
            var fake = new Fake<TestClass>();
            fake.Options.AllowedVirtualMembers.Add(m =>
                m.DeclaringType is "AutoFake.FunctionalTests.VirtualTests/TestClass" or "AutoFake.FunctionalTests.VirtualTests/TestClass/Derived" &&
                m.ReturnType == "System.Int32" &&
                m.Name == nameof(TestClass.GetNumber) &&
                m.ParameterTypes.Length == 0);

            var sut = fake.Rewrite(f => f.GetVirtualNumberByMethod());
            sut.Replace(() => NumberFactory.Get()).Return(2);

            Assert.Equal(2, sut.Execute());
        }

        [Fact]
        public void When_virtual_method_with_all_virtual_members_Should_execute_overriden_method()
        {
            var fake = new Fake<TestClass>();

            var sut = fake.Rewrite(f => f.GetVirtualNumberByMethod());
            sut.Replace(() => NumberFactory.Get()).Return(2);

            Assert.Equal(2, sut.Execute());
        }

        [Fact]
        public void When_virtual_property_with_exact_name_configured_Should_execute_overriden_method()
        {
            var fake = new Fake<TestClass>();
            fake.Options.AllowedVirtualMembers.Add(m => m.Name == "get_" + nameof(TestClass.Number));

            var sut = fake.Rewrite(f => f.GetVirtualNumberByProperty());
            sut.Replace(() => NumberFactory.Get()).Return(4);

            Assert.Equal(4, sut.Execute());
        }

        [Fact]
        public void When_virtual_property_with_all_virtual_members_Should_execute_overriden_method()
        {
            var fake = new Fake<TestClass>();

            var sut = fake.Rewrite(f => f.GetVirtualNumberByProperty());
            sut.Replace(() => NumberFactory.Get()).Return(4);

            Assert.Equal(4, sut.Execute());
        }

        private class TestClass
        {
            public int GetVirtualNumberByMethod() => GetDerived().GetNumber();
            public int GetVirtualNumberByProperty() => GetDerived().Number;

            private TestClass GetDerived() => new Derived();

            public virtual int Number => 3;
            public virtual int GetNumber() => 1;

            private class Derived : TestClass
            {
                public override int Number => NumberFactory.Get();
                public override int GetNumber() => NumberFactory.Get();
            }
        }

        private static class NumberFactory
        {
            public static int Get() => 10;
        }
    }
}
