using System;
using System.Data.SqlClient;
using System.IO;
using Xunit;

namespace AutoFake.IntegrationTests.InstanceTests
{
    public class FieldMockTests
    {
        [Fact]
        public void OwnInstanceTest()
        {
            var fake = new Fake<TestClass>();
        
            fake.Replace(t => t.DynamicValue).Returns(7);

            Assert.Equal(7, fake.Rewrite(f => f.GetDynamicValue()).Execute());
        }

        [Fact]
        public void ExternalInstanceTest()
        {
            var fake = new Fake<TestClass>();

            fake.Replace((HelperClass h) => h.DynamicValue).Returns(7);

            Assert.Equal(7, fake.Rewrite(f => f.GetHelperDynamicValue()).Execute());
        }

        [Fact]
        public void OwnStaticTest()
        {
            var fake = new Fake<TestClass>();

            fake.Replace(() => TestClass.DynamicStaticValue).Returns(7);

            Assert.Equal(7, fake.Rewrite(f => f.GetDynamicStaticValue()).Execute());
        }

        [Fact]
        public void ExternalStaticTest()
        {
            var fake = new Fake<TestClass>();

            fake.Replace(() => HelperClass.DynamicStaticValue).Returns(7);

            Assert.Equal(7, fake.Rewrite(f => f.GetHelperDynamicStaticValue()).Execute());
        }

        [Fact]
        public void FrameworkStaticTest()
        {
            var fake = new Fake<TestClass>();
            
            var textReader = new StringReader(string.Empty);
            fake.Replace(() => TextReader.Null).Returns(textReader);

            var actual = fake.Rewrite(f => f.GetFrameworkStaticValue()).Execute();
            Assert.Equal(textReader, actual);
            Assert.NotEqual(TextReader.Null, actual);
        }

        [Fact]
        public void StructFieldByAddress()
        {
            var fake = new Fake<TestClass>();
            var expected = new HelperStruct {Value = 5};
            fake.Replace(f => f.StructValue).Returns(expected);

            var actual = fake.Rewrite(f => f.GetStructValueByAddress()).Execute();

            Assert.Equal(expected.Value, actual);
        }

        [Fact]
        public void StaticStructFieldByAddress()
        {
            var fake = new Fake<TestClass>();
            var expected = new HelperStruct { Value = 5 };
            fake.Replace(() => TestClass.StaticStructValue).Returns(expected);

            var actual = fake.Rewrite(f => f.GetStaticStructValueByAddress()).Execute();

            Assert.Equal(expected.Value, actual);
        }

        private class TestClass
        {
            public int DynamicValue = 5;
            public static int DynamicStaticValue = 5;
            public HelperStruct StructValue;
            public static HelperStruct StaticStructValue;

            public int GetDynamicValue()
            {
                Console.WriteLine("Started");
                var value = DynamicValue;
                Console.WriteLine("Finished");
                return value;
            }

            public int GetHelperDynamicValue()
            {
                Console.WriteLine("Started");
                var helper = new HelperClass();
                var value = helper.DynamicValue;
                Console.WriteLine("Finished");
                return value;
            }

            public int GetDynamicStaticValue()
            {
                Console.WriteLine("Started");
                var value = DynamicStaticValue;
                Console.WriteLine("Finished");
                return value;
            }

            public int GetHelperDynamicStaticValue()
            {
                Console.WriteLine("Started");
                var value = HelperClass.DynamicStaticValue;
                Console.WriteLine("Finished");
                return value;
            }

            public string GetFrameworkValue()
            {
                Console.WriteLine("Started");
                var cmd = new SqlCommand();
                var value = cmd.CommandText;
                Console.WriteLine("Finished");
                return value;
            }
            public TextReader GetFrameworkStaticValue()
            {
                Console.WriteLine("Started");
                var value = TextReader.Null;
                Console.WriteLine("Finished");
                return value;
            }

            public int GetStructValueByAddress()
            {
                Console.WriteLine("Started");
                var value = StructValue.Value;
                Console.WriteLine("Finished");
                return value;
            }

            public int GetStaticStructValueByAddress()
            {
                Console.WriteLine("Started");
                var value = StaticStructValue.Value;
                Console.WriteLine("Finished");
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
