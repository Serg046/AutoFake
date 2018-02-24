using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using Xunit;

namespace AutoFake.IntegrationTests.StaticTests
{
    public class FieldMockTests
    {
        [Fact]
        public void OwnStaticTest()
        {
            var fake = new Fake(typeof(TestClass));

            fake.Replace(() => TestClass.DynamicStaticValue).Returns(7);

            Assert.Equal(7, fake.Rewrite(() => TestClass.GetDynamicStaticValue()).Execute());
        }

        [Fact]
        public void ExternalStaticTest()
        {
            var fake = new Fake(typeof(TestClass));

            fake.Replace(() => HelperClass.DynamicStaticValue).Returns(7);

            Assert.Equal(7, fake.Rewrite(() => TestClass.GetHelperDynamicStaticValue()).Execute());
        }

        [Fact]
        public void FrameworkInstanceTest()
        {
            var fake = new Fake(typeof(TestClass));

            var cmd = "select * from Test";
            fake.Replace((SqlCommand c) => c.CommandText).Returns(cmd);

            Assert.Equal(cmd, fake.Rewrite(() => TestClass.GetFrameworkValue()).Execute());
        }

        [Fact]
        public void FrameworkStaticTest()
        {
            var fake = new Fake(typeof(TestClass));

            var textReader = new StringReader(string.Empty);
            fake.Replace(() => TextReader.Null).Returns(textReader);

            var actual = fake.Rewrite(() => TestClass.GetFrameworkStaticValue()).Execute();
            Assert.Equal(textReader, actual);
            Assert.NotEqual(TextReader.Null, actual);
        }

        [Fact]
        public void StaticStructFieldByAddress()
        {
            var fake = new Fake(typeof(TestClass));
            var expected = new HelperStruct { Value = 5 };
            fake.Replace(() => TestClass.StaticStructValue).Returns(expected);

            var actual = fake.Rewrite(() => TestClass.GetStaticStructValueByAddress()).Execute();

            Assert.Equal(expected.Value, actual);
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
