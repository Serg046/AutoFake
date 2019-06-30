using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace AutoFake.IntegrationTests.InstanceTests
{
    public class PropertyMockTests
    {
        [Fact]
        public void OwnInstanceTest()
        {
            var fake = new Fake<TestClass>();

            fake.Replace(t => t.DynamicValue).Returns(() => 7);
            fake.Rewrite(f => f.GetDynamicValue());

            fake.Execute(tst => Assert.Equal(7, tst.GetDynamicValue()));
        }

        [Fact]
        public void ExternalInstanceTest()
        {
            var fake = new Fake<TestClass>();

            fake.Replace((HelperClass h) => h.DynamicValue).Returns(() => 7);
            fake.Rewrite(f => f.GetHelperDynamicValue());

            fake.Execute(tst => Assert.Equal(7, tst.GetHelperDynamicValue()));
        }

        [Fact]
        public void OwnStaticTest()
        {
            var fake = new Fake<TestClass>();

            fake.Replace(() => TestClass.DynamicStaticValue).Returns(() => 7);
            fake.Rewrite(f => f.GetDynamicStaticValue());

            fake.Execute(tst => Assert.Equal(7, tst.GetDynamicStaticValue()));
        }

        [Fact]
        public void ExternalStaticTest()
        {
            var fake = new Fake<TestClass>();

            fake.Replace(() => HelperClass.DynamicStaticValue).Returns(() => 7);
            fake.Rewrite(f => f.GetHelperDynamicStaticValue());

            fake.Execute(tst => Assert.Equal(7, tst.GetHelperDynamicStaticValue()));
        }

        [Fact]
        public void FrameworkInstanceTest()
        {
            var fake = new Fake<TestClass>();

            const string cmd = "select * from Test";
            fake.Replace((SqlCommand c) => c.CommandText).Returns(() => cmd);
            fake.Rewrite(f => f.GetFrameworkValue());

            fake.Execute(tst => Assert.Equal(cmd, tst.GetFrameworkValue()));
        }

        [Fact]
        public void FrameworkStaticTest()
        {
            var fake = new Fake<TestClass>();

            fake.Replace(() => DateTime.Now).Returns(() => new DateTime(2016, 9, 25));
            fake.Rewrite(f => f.GetFrameworkStaticValue());

            fake.Execute((tst, prms) => Assert.Equal(prms.Single(), tst.GetFrameworkStaticValue()));
        }

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
    }
}
