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
            fake.Replace(() => DateTime.Now).Returns(DateTime.Now);
            fake.Rewrite(f => f.GetClassCtorResult());

            fake.Execute2(tst => Assert.Equal(7, tst.GetClassCtorResult().Prop));
        }

        [Fact]
        public void ClassCtorWithArgsTest()
        {
            var fake = new Fake<TestClass>();
            fake.Replace(() => DateTime.Now).Returns(DateTime.Now);
            fake.Rewrite(f => f.GetClassCtorWithArgsResult());

            fake.Execute2(tst => Assert.Equal(7, tst.GetClassCtorWithArgsResult().Prop));
        }

        [Fact]
        public void ClassFieldTest()
        {
            var fake = new Fake<TestClass>();
            fake.Replace(() => DateTime.Now).Returns(DateTime.Now);
            fake.Rewrite(f => f.GetClassField());

            fake.Execute2(tst => Assert.Equal(7, tst.GetClassField().Prop));
        }

        [Fact]
        public void StructFieldTest()
        {
            var fake = new Fake<TestClass>();
            fake.Replace(() => DateTime.Now).Returns(DateTime.Now);
            fake.Rewrite(f => f.GetStructField());

            fake.Execute2(tst => Assert.Equal(7, tst.GetStructField().Prop));
        }

        public class HelperClass
        {
            public HelperClass() { }
            public HelperClass(int arg1, string arg2) { }

            public int Prop { get; set; }
        }
        
        public struct HelperStruct
        {
            public int Prop { get; set; }
        }

        public class TestClass
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
