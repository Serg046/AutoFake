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

            var sut = fake.Rewrite(() => TestClass.GetDynamicStaticValue());
            sut.Replace(() => TestClass.DynamicStaticValue).Return(() => 7);

            Assert.Equal(7, sut.Execute());
        }

        [Fact]
        public void ExternalStaticTest()
        {
            var fake = new Fake(typeof(TestClass));

            var sut = fake.Rewrite(() => TestClass.GetHelperDynamicStaticValue());
            sut.Replace(() => HelperClass.DynamicStaticValue).Return(() => 7);

            Assert.Equal(7, sut.Execute());
        }

        [Fact]
        public void FrameworkInstanceTest()
        {
            var fake = new Fake(typeof(TestClass));

            const string cmd = "select * from Test";
            var sut = fake.Rewrite(() => TestClass.GetFrameworkValue());
            sut.Replace((SqlCommand c) => c.CommandText).Return(() => cmd);

            Assert.Equal(cmd, sut.Execute());
        }

        [Fact]
        public void FrameworkStaticTest()
        {
            var fake = new Fake(typeof(TestClass));

            var sr = new StringReader(string.Empty);
            var sut = fake.Rewrite(() => TestClass.GetFrameworkStaticValue());
            sut.Replace(() => TextReader.Null).Return(sr);

            var actual = sut.Execute();
            Assert.Equal(sr, actual);
            Assert.NotEqual(TextReader.Null, actual);
        }

        [Fact]
        public void StaticStructFieldByAddress()
        {
            var fake = new Fake(typeof(TestClass));

            const int value = 5;
            var data = new HelperStruct {Value = value};
            var sut = fake.Rewrite(() => TestClass.GetStaticStructValueByAddress());
            sut.Replace(() => TestClass.StaticStructValue).Return(data);

            Assert.Equal(value, sut.Execute());
        }

#pragma warning disable 0649
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
