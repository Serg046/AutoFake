using System;
using System.Data.SqlClient;
using System.Diagnostics;
using Xunit;

namespace AutoFake.IntegrationTests.InstanceTests
{
    public class PropertyMockTests
    {
        private class TestClass
        {
            public int DynamicValue => 5;
            public static int DynamicStaticValue => 5;

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
                var cmd = new SqlCommand();
                var vaue = cmd.CommandText;
                Debug.WriteLine("Finished");
                return vaue;
            }
            public DateTime GetFrameworkStaticValue()
            {
                Debug.WriteLine("Started");
                var vaue = DateTime.Now;
                Debug.WriteLine("Finished");
                return vaue;
            }
        }

        private class HelperClass
        {
            public int DynamicValue => 5;
            public static int DynamicStaticValue => 5;
        }

        [Fact]
        public void OwnInstanceTest()
        {
            var fake = new Fake<TestClass>();

            fake.Replace((TestClass t) => t.DynamicValue).Returns(7);

            Assert.Equal(7, fake.Execute(f => f.GetDynamicValue()));
        }

        [Fact]
        public void ExternalInstanceTest()
        {
            var fake = new Fake<TestClass>();

            fake.Replace((HelperClass h) => h.DynamicValue).Returns(7);

            Assert.Equal(7, fake.Execute(f => f.GetHelperDynamicValue()));
        }

        [Fact]
        public void OwnStaticTest()
        {
            var fake = new Fake<TestClass>();

            fake.Replace(() => TestClass.DynamicStaticValue).Returns(7);

            Assert.Equal(7, fake.Execute(f => f.GetDynamicStaticValue()));
        }

        [Fact]
        public void ExternalStaticTest()
        {
            var fake = new Fake<TestClass>();

            fake.Replace(() => HelperClass.DynamicStaticValue).Returns(7);

            Assert.Equal(7, fake.Execute(f => f.GetHelperDynamicStaticValue()));
        }

        [Fact]
        public void FrameworkInstanceTest()
        {
            var fake = new Fake<TestClass>();

            var cmd = "select * from Test";
            fake.Replace((SqlCommand c) => c.CommandText).Returns(cmd);

            Assert.Equal(cmd, fake.Execute(f => f.GetFrameworkValue()));
        }

        [Fact]
        public void FrameworkStaticTest()
        {
            var fake = new Fake<TestClass>();

            var date = new DateTime(2016, 9, 25);
            fake.Replace(() => DateTime.Now).Returns(date);

            Assert.Equal(date, fake.Execute(f => f.GetFrameworkStaticValue()));
        }
    }
}
