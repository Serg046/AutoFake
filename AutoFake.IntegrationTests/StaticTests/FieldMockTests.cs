using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;

namespace AutoFake.IntegrationTests.StaticTests
{
    public class FieldMockTests
    {
        [Fact]
        public void OwnStaticTest()
        {
            var fake = new Fake(typeof(TestClass));

            fake.Replace(() => TestClass.DynamicStaticValue).Returns(() => 7);
            fake.Rewrite(() => TestClass.GetDynamicStaticValue());

            fake.Execute2(tst => Assert.Equal(7, tst.Execute(() => TestClass.GetDynamicStaticValue())));
        }

        [Fact]
        public void ExternalStaticTest()
        {
            var fake = new Fake(typeof(TestClass));

            fake.Replace(() => HelperClass.DynamicStaticValue).Returns(() => 7);
            fake.Rewrite(() => TestClass.GetHelperDynamicStaticValue());

            fake.Execute2(tst => Assert.Equal(7, tst.Execute(() => TestClass.GetHelperDynamicStaticValue())));
        }

        [Fact]
        public void FrameworkInstanceTest()
        {
            var fake = new Fake(typeof(TestClass));

            const string cmd = "select * from Test";
            fake.Replace((SqlCommand c) => c.CommandText).Returns(() => cmd);
            fake.Rewrite(() => TestClass.GetFrameworkValue());

            fake.Execute2(tst => Assert.Equal(cmd, tst.Execute(() => TestClass.GetFrameworkValue())));
        }

        [Fact]
        public void FrameworkStaticTest()
        {
            var fake = new Fake(typeof(TestClass));

            fake.Replace(() => TextReader.Null).Returns(() => new StringReader(string.Empty));
            fake.Rewrite(() => TestClass.GetFrameworkStaticValue());

            fake.Execute2((tst, prms) =>
            {
                var actual = tst.Execute(() => TestClass.GetFrameworkStaticValue());
                Assert.Equal(prms.Single(), actual);
                Assert.NotEqual(TextReader.Null, actual);
            });
        }

        [Fact]
        public void StaticStructFieldByAddress()
        {
            var fake = new Fake(typeof(TestClass));

            const int value = 5;
            fake.Replace(() => TestClass.StaticStructValue).Returns(() => new HelperStruct { Value = value });
            fake.Rewrite(() => TestClass.GetStaticStructValueByAddress());

            fake.Execute2(tst => Assert.Equal(value, tst.Execute(() => TestClass.GetStaticStructValueByAddress())));
        }

        private static class TestClass
        {
            public static int DynamicStaticValue = 5;
            public static HelperStruct StaticStructValue;

            public static int GetDynamicStaticValue()
            {
                Debug.WriteLine("Started");
                var value = DynamicStaticValue;
                Debug.WriteLine("Finished");
                return value;
            }

            public static int GetHelperDynamicStaticValue()
            {
                Debug.WriteLine("Started");
                var value = HelperClass.DynamicStaticValue;
                Debug.WriteLine("Finished");
                return value;
            }

            public static string GetFrameworkValue()
            {
                Debug.WriteLine("Started");
                var cmd = new SqlCommand();
                var value = cmd.CommandText;
                Debug.WriteLine("Finished");
                return value;
            }

            public static TextReader GetFrameworkStaticValue()
            {
                Debug.WriteLine("Started");
                var value = TextReader.Null;
                Debug.WriteLine("Finished");
                return value;
            }

            public static int GetStaticStructValueByAddress()
            {
                Debug.WriteLine("Started");
                var value = StaticStructValue.Value;
                Debug.WriteLine("Finished");
                return value;
            }
        }

        private static class HelperClass
        {
            public static int DynamicStaticValue = 5;
        }

        public struct HelperStruct
        {
            public int Value;
        }
    }
}
