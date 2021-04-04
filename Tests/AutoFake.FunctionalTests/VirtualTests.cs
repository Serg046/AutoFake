using Xunit;

namespace AutoFake.FunctionalTests
{
    public class VirtualTests
    {
        [Fact]
        public void When_VirtualMethodWithExactNameConfigured_Should_ExecuteOverridingMethod()
        {
            var fake = new Fake<TestClass>();
            fake.Options.VirtualMembers.Add(nameof(TestClass.GetNumber));

            var sut = fake.Rewrite(f => f.GetVirtualNumberByMethod());
            sut.Replace(() => NumberFactory.Get()).Return(2);

            Assert.Equal(2, sut.Execute());
        }

        [Fact]
        public void When_VirtualMethodWithAllVirtualMembers_Should_ExecuteOverridingMethod()
        {
            var fake = new Fake<TestClass>();
            fake.Options.IncludeAllVirtualMembers = true;

            var sut = fake.Rewrite(f => f.GetVirtualNumberByMethod());
            sut.Replace(() => NumberFactory.Get()).Return(2);

            Assert.Equal(2, sut.Execute());
        }

        [Fact]
        public void When_VirtualPropertyWithExactNameConfigured_Should_ExecuteOverridingMethod()
        {
            var fake = new Fake<TestClass>();
            fake.Options.VirtualMembers.Add("get_" + nameof(TestClass.Number));

            var sut = fake.Rewrite(f => f.GetVirtualNumberByProperty());
            sut.Replace(() => NumberFactory.Get()).Return(4);

            Assert.Equal(4, sut.Execute());
        }

        [Fact]
        public void When_VirtualPropertyWithAllVirtualMembers_Should_ExecuteOverridingMethod()
        {
            var fake = new Fake<TestClass>();
            fake.Options.IncludeAllVirtualMembers = true;

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
