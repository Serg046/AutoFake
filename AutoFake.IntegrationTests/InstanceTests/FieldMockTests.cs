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
        
            var sut = fake.Rewrite(f => f.GetDynamicValue());
            sut.Replace(t => t.DynamicValue).Return(() => 7);

            Assert.Equal(7, sut.Execute());
        }

        [Fact]
        public void ExternalInstanceTest()
        {
            var fake = new Fake<TestClass>();

            var sut = fake.Rewrite(f => f.GetHelperDynamicValue());
            sut.Replace((HelperClass h) => h.DynamicValue).Return(() => 7);

            Assert.Equal(7, sut.Execute());
        }

        [Fact]
        public void OwnStaticTest()
        {
            var fake = new Fake<TestClass>();

            var sut = fake.Rewrite(f => f.GetDynamicStaticValue());
            sut.Replace(() => TestClass.DynamicStaticValue).Return(() => 7);

            Assert.Equal(7, sut.Execute());
        }

        [Fact]
        public void ExternalStaticTest()
        {
            var fake = new Fake<TestClass>();

            var sut = fake.Rewrite(f => f.GetHelperDynamicStaticValue());
            sut.Replace(() => HelperClass.DynamicStaticValue).Return(() => 7);

            Assert.Equal(7, sut.Execute());
        }

        [Fact]
        public void FrameworkTest()
        {
            var fake = new Fake<TestClass>();

            const string header = "Test header";
            var sut = fake.Rewrite(f => f.GetFrameworkValue());
            sut.Replace((Header hd) => hd.Name).Return(() => header);

            Assert.Equal(header, sut.Execute());
        }

        [Fact]
        public void FrameworkStaticTest()
        {
            var fake = new Fake<TestClass>();

            var sr = new StringReader(string.Empty);
            var sut = fake.Rewrite(f => f.GetFrameworkStaticValue());
            sut.Replace(() => TextReader.Null).Return(sr);

            var actual = sut.Execute();
            Assert.Equal(sr, actual);
            Assert.NotEqual(TextReader.Null, actual);
        }

        [Fact]
        public void StructFieldByAddress()
        {
            var fake = new Fake<TestClass>();

            var data = new HelperStruct {Value = 5};
            var sut = fake.Rewrite(f => f.GetStructValueByAddress());
            sut.Replace(f => f.StructValue).Return(data);

            Assert.Equal(data.Value, sut.Execute());
        }

        [Fact]
        public void StaticStructFieldByAddress()
        {
            var fake = new Fake<TestClass>();

            var data = new HelperStruct {Value = 5};
            var sut = fake.Rewrite(f => f.GetStaticStructValueByAddress());
            sut.Replace(() => TestClass.StaticStructValue).Return(data);

            Assert.Equal(data.Value, sut.Execute());
        }

#pragma warning disable 0649
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

#if NETCOREAPP
namespace System.Runtime.Remoting.Messaging
{
    public class Header
    {
        public string Name;
        public object Value;

        public Header(string name, object value)
        {
            Name = name;
            Value = value;
        }
    }
}
#endif
