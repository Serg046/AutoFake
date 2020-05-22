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

            fake.Rewrite(() => TestClass.GetDynamicStaticValue())
                .Replace(() => TestClass.DynamicStaticValue).Return(() => 7);

            fake.Execute(() => Assert.Equal(7, TestClass.GetDynamicStaticValue()));
        }

        [Fact]
        public void ExternalStaticTest()
        {
            var fake = new Fake(typeof(TestClass));

            fake.Rewrite(() => TestClass.GetHelperDynamicStaticValue())
                .Replace(() => HelperClass.DynamicStaticValue).Return(() => 7);

            fake.Execute(() => Assert.Equal(7, TestClass.GetHelperDynamicStaticValue()));
        }

        [Fact]
        public void FrameworkInstanceTest()
        {
            var fake = new Fake(typeof(TestClass));

            const string cmd = "select * from Test";
            fake.Rewrite(() => TestClass.GetFrameworkValue())
                .Replace((SqlCommand c) => c.CommandText).Return(() => cmd);

            fake.Execute(() => Assert.Equal(cmd, TestClass.GetFrameworkValue()));
        }

        [Fact]
        public void FrameworkStaticTest()
        {
            var fake = new Fake(typeof(TestClass));

            fake.Rewrite(() => TestClass.GetFrameworkStaticValue())
                .Replace(() => TextReader.Null).Return(() => new StringReader(string.Empty));

            fake.Execute((tst, prms) =>
            {
                var actual = TestClass.GetFrameworkStaticValue();
                Assert.Equal(prms.Single(), actual);
                Assert.NotEqual(TextReader.Null, actual);
            });
        }

        [Fact]
        public void StaticStructFieldByAddress()
        {
            var fake = new Fake(typeof(TestClass));

            const int value = 5;
            fake.Rewrite(() => TestClass.GetStaticStructValueByAddress())
                .Replace(() => TestClass.StaticStructValue).Return(() => new HelperStruct { Value = value });

            fake.Execute(() => Assert.Equal(value, TestClass.GetStaticStructValueByAddress()));
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

        private struct HelperStruct
        {
            public int Value;
        }
    }
}
