using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace AutoFake.IntegrationTests.StaticTests
{
    public class PropertyMockTests
    {
        [Fact]
        public void OwnStaticTest()
        {
            var fake = new Fake(typeof(TestClass));

            fake.Rewrite(() => TestClass.GetDynamicStaticValue())
                .Replace(() => TestClass.DynamicStaticValue).Return(() => 7);

            fake.Execute(tst => Assert.Equal(7, TestClass.GetDynamicStaticValue()));
        }

        [Fact]
        public void ExternalStaticTest()
        {
            var fake = new Fake(typeof(TestClass));

            fake.Rewrite(() => TestClass.GetHelperDynamicStaticValue())
                .Replace(() => HelperClass.DynamicStaticValue).Return(() => 7);

            fake.Execute(tst => Assert.Equal(7, TestClass.GetHelperDynamicStaticValue()));
        }

        [Fact]
        public void FrameworkInstanceTest()
        {
            var fake = new Fake(typeof(TestClass));

            const string cmd = "select * from Test";
            fake.Rewrite(() => TestClass.GetFrameworkValue())
                .Replace((SqlCommand c) => c.CommandText).Return(() => cmd);

            fake.Execute(tst => Assert.Equal(cmd, TestClass.GetFrameworkValue()));
        }

        [Fact]
        public void FrameworkStaticTest()
        {
            var fake = new Fake(typeof(TestClass));

            fake.Rewrite(() => TestClass.GetFrameworkStaticValue())
                .Replace(() => DateTime.Now).Return(() => new DateTime(2016, 9, 25));

            fake.Execute((tst, prms) => Assert.Equal(prms.Single(), TestClass.GetFrameworkStaticValue()));
        }

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
    }
}
