using System;
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

        public class HelperClass
        {
            public HelperClass() { }
            public HelperClass(int arg1, string arg2) { }

            public int Prop { get; set; }
        }
        
        private struct HelperStruct
        {
            public int Prop { get; set; }
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
        }
    }
}
