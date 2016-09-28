using System;
using System.Data.SqlClient;
using System.Diagnostics;
using Xunit;

namespace AutoFake.IntegrationTests.StaticTests
{
    public class PropertyMockTests
    {
        private static class TestClass
        {
            public static int DynamicStaticValue => 5;

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
                var vaue = cmd.CommandText;
                Debug.WriteLine("Finished");
                return vaue;
            }
            public static DateTime GetFrameworkStaticValue()
            {
                Debug.WriteLine("Started");
                var vaue = DateTime.Now;
                Debug.WriteLine("Finished");
                return vaue;
            }
        }

        private static class HelperClass
        {
            public static int DynamicStaticValue => 5;
        }

        [Fact]
        public void OwnStaticTest()
        {
            var fake = new Fake(typeof(TestClass));

            fake.Replace(() => TestClass.DynamicStaticValue).Returns(7);

            Assert.Equal(7, fake.Execute(() => TestClass.GetDynamicStaticValue()));
        }

        [Fact]
        public void ExternalStaticTest()
        {
            var fake = new Fake(typeof(TestClass));

            fake.Replace(() => HelperClass.DynamicStaticValue).Returns(7);

            Assert.Equal(7, fake.Execute(() => TestClass.GetHelperDynamicStaticValue()));
        }

        [Fact]
        public void FrameworkInstanceTest()
        {
            var fake = new Fake(typeof(TestClass));

            var cmd = "select * from Test";
            fake.Replace((SqlCommand c) => c.CommandText).Returns(cmd);

            Assert.Equal(cmd, fake.Execute(() => TestClass.GetFrameworkValue()));
        }

        [Fact]
        public void FrameworkStaticTest()
        {
            var fake = new Fake(typeof(TestClass));

            var date = new DateTime(2016, 9, 25);
            fake.Replace(() => DateTime.Now).Returns(date);

            Assert.Equal(date, fake.Execute(() => TestClass.GetFrameworkStaticValue()));
        }
    }
}
