using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using Xunit;

namespace AutoFake.IntegrationTests.InstanceTests
{
    public class FieldMockTests
    {
        [Fact]
        public void OwnInstanceTest()
        {
            var fake = new Fake<TestClass>();
        
            fake.Replace(t => t.DynamicValue).Returns(() => 7);
            fake.Rewrite(f => f.GetDynamicValue());

            fake.Execute(tst => Assert.Equal(7, tst.GetDynamicValue()));
        }

        [Fact]
        public void ExternalInstanceTest()
        {
            var fake = new Fake<TestClass>();

            fake.Replace((HelperClass h) => h.DynamicValue).Returns(() => 7);
            fake.Rewrite(f => f.GetHelperDynamicValue());

            fake.Execute(tst => Assert.Equal(7, tst.GetHelperDynamicValue()));
        }

        [Fact]
        public void OwnStaticTest()
        {
            var fake = new Fake<TestClass>();

            fake.Replace(() => TestClass.DynamicStaticValue).Returns(() => 7);
            fake.Rewrite(f => f.GetDynamicStaticValue());

            fake.Execute(tst => Assert.Equal(7, tst.GetDynamicStaticValue()));
        }

        [Fact]
        public void ExternalStaticTest()
        {
            var fake = new Fake<TestClass>();

            fake.Replace(() => HelperClass.DynamicStaticValue).Returns(() => 7);
            fake.Rewrite(f => f.GetHelperDynamicStaticValue());

            fake.Execute(tst => Assert.Equal(7, tst.GetHelperDynamicStaticValue()));
        }

        [Fact]
        public void FrameworkTest()
        {
            var fake = new Fake<TestClass>();

            const string header = "Test header";
            fake.Replace((Header hd) => hd.Name).Returns(() => header);
            fake.Rewrite(f => f.GetFrameworkValue());

            fake.Execute(tst => Assert.Equal(header, tst.GetFrameworkValue()));
        }

        [Fact]
        public void FrameworkStaticTest()
        {
            var fake = new Fake<TestClass>();

            fake.Replace(() => TextReader.Null).Returns(() => new StringReader(string.Empty));
            fake.Rewrite(f => f.GetFrameworkStaticValue());

            fake.Execute((tst, prms) =>
            {
                var actual = tst.GetFrameworkStaticValue();
                Assert.Equal(prms.Single(), actual);
                Assert.NotEqual(TextReader.Null, actual);
            });
        }

        [Fact]
        public void StructFieldByAddress()
        {
            var fake = new Fake<TestClass>();

            fake.Replace(f => f.StructValue).Returns(() => new HelperStruct {Value = 5});
            fake.Rewrite(f => f.GetStructValueByAddress());

            fake.Execute((tst, prms) => Assert.Equal(((HelperStruct)prms.Single()).Value,
                tst.GetStructValueByAddress()));
        }

        [Fact]
        public void StaticStructFieldByAddress()
        {
            var fake = new Fake<TestClass>();

            fake.Replace(() => TestClass.StaticStructValue).Returns(() => new HelperStruct { Value = 5 });
            fake.Rewrite(f => f.GetStaticStructValueByAddress());

            fake.Execute((tst, prms) => Assert.Equal(((HelperStruct)prms.Single()).Value,
                tst.GetStaticStructValueByAddress()));
        }

        public class TestClass
        {
            public int DynamicValue = 5;
            public static int DynamicStaticValue = 5;
            public HelperStruct StructValue;
            public static HelperStruct StaticStructValue;

            public int GetDynamicValue()
            {
                Debug.WriteLine("Started");
                var value = DynamicValue;
                Debug.WriteLine("Finished");
                return value;
            }

            public int GetHelperDynamicValue()
            {
                Debug.WriteLine("Started");
                var helper = new HelperClass();
                var value = helper.DynamicValue;
                Debug.WriteLine("Finished");
                return value;
            }

            public int GetDynamicStaticValue()
            {
                Debug.WriteLine("Started");
                var value = DynamicStaticValue;
                Debug.WriteLine("Finished");
                return value;
            }

            public int GetHelperDynamicStaticValue()
            {
                Debug.WriteLine("Started");
                var value = HelperClass.DynamicStaticValue;
                Debug.WriteLine("Finished");
                return value;
            }

            public string GetFrameworkValue()
            {
                Debug.WriteLine("Started");
                var header = new Header("header", 5);
                var value = header.Name;
                Debug.WriteLine("Finished");
                return value;
            }
            public TextReader GetFrameworkStaticValue()
            {
                Debug.WriteLine("Started");
                var value = TextReader.Null;
                Debug.WriteLine("Finished");
                return value;
            }

            public int GetStructValueByAddress()
            {
                Debug.WriteLine("Started");
                var value = StructValue.Value;
                Debug.WriteLine("Finished");
                return value;
            }

            public int GetStaticStructValueByAddress()
            {
                Debug.WriteLine("Started");
                var value = StaticStructValue.Value;
                Debug.WriteLine("Finished");
                return value;
            }
        }

        private class HelperClass
        {
            public int DynamicValue = 5;
            public static int DynamicStaticValue = 5;
        }

        public struct HelperStruct
        {
            public int Value;
        }
    }
}
