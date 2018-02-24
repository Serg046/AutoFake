using System.Diagnostics;
using System.IO;
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
        public void FrameworkTest()
        {
            var fake = new Fake<TestClass>();

            var header = "Test header";
            fake.Replace((Header hd) => hd.Name).Returns(header);

            var actual = fake.Rewrite(f => f.GetFrameworkValue()).Execute();
            Assert.Equal(header, actual);
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
