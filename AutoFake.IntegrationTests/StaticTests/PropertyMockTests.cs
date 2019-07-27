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

            fake.Replace(() => TestClass.DynamicStaticValue).Return(() => 7);
            fake.Rewrite(() => TestClass.GetDynamicStaticValue());

            fake.Execute(tst => Assert.Equal(7, tst.Execute(() => TestClass.GetDynamicStaticValue())));
        }

        [Fact]
        public void ExternalStaticTest()
        {
            var fake = new Fake(typeof(TestClass));

            fake.Replace(() => HelperClass.DynamicStaticValue).Return(() => 7);
            fake.Rewrite(() => TestClass.GetHelperDynamicStaticValue());

            fake.Execute(tst => Assert.Equal(7, tst.Execute(() => TestClass.GetHelperDynamicStaticValue())));
        }

        [Fact]
        public void FrameworkInstanceTest()
        {
            var fake = new Fake(typeof(TestClass));

            const string cmd = "select * from Test";
            fake.Replace((SqlCommand c) => c.CommandText).Return(() => cmd);
            fake.Rewrite(() => TestClass.GetFrameworkValue());

            fake.Execute(tst => Assert.Equal(cmd, tst.Execute(() => TestClass.GetFrameworkValue())));
        }

        [Fact]
        public void FrameworkStaticTest()
        {
            var fake = new Fake(typeof(TestClass));

            fake.Replace(() => DateTime.Now).Return(() => new DateTime(2016, 9, 25));
            fake.Rewrite(() => TestClass.GetFrameworkStaticValue());

            fake.Execute((tst, prms) => Assert.Equal(prms.Single(), tst.Execute(() => TestClass.GetFrameworkStaticValue())));
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
