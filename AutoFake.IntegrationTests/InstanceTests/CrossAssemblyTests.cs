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
            fake.Rewrite(f => f.GetClassCtorResult())
                .Replace(() => DateTime.Now).Return(() => DateTime.Now);

            fake.Execute(tst => Assert.Equal(7, tst.GetClassCtorResult().Prop));
        }

        [Fact]
        public void ClassCtorWithArgsTest()
        {
            var fake = new Fake<TestClass>();
            fake.Rewrite(f => f.GetClassCtorWithArgsResult())
                .Replace(() => DateTime.Now).Return(() => DateTime.Now);

            fake.Execute(tst => Assert.Equal(7, tst.GetClassCtorWithArgsResult().Prop));
        }

        [Fact]
        public void ClassFieldTest()
        {
            var fake = new Fake<TestClass>();
            fake.Rewrite(f => f.GetClassField())
                .Replace(() => DateTime.Now).Return(() => DateTime.Now);

            fake.Execute(tst => Assert.Equal(7, tst.GetClassField().Prop));
        }

        [Fact]
        public void StructFieldTest()
        {
            var fake = new Fake<TestClass>();
            fake.Rewrite(f => f.GetStructField())
                .Replace(() => DateTime.Now).Return(() => DateTime.Now);

            fake.Execute(tst => Assert.Equal(7, tst.GetStructField().Prop));
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
