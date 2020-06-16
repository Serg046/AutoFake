using System;
using System.Data.SqlClient;
using System.Diagnostics;
using Xunit;

namespace AutoFake.IntegrationTests.InstanceTests
{
    public class PropertyMockTests
    {
        [Fact]
        public void OwnInstanceTest()
        {
            var fake = new Fake<TestClass>();

            var sut = fake.Rewrite(f => f.GetDynamicValue());
            sut.Replace(t => t.DynamicValue).Return(7);

            Assert.Equal(7, sut.Execute());
        }

        [Fact]
        public void ExternalInstanceTest()
        {
            var fake = new Fake<TestClass>();

            var sut = fake.Rewrite(f => f.GetHelperDynamicValue());
            sut.Replace((HelperClass h) => h.DynamicValue).Return(7);

            Assert.Equal(7, sut.Execute());
        }

        [Fact]
        public void OwnStaticTest()
        {
            var fake = new Fake<TestClass>();

            var sut = fake.Rewrite(f => f.GetDynamicStaticValue());
            sut.Replace(() => TestClass.DynamicStaticValue).Return(7);

            Assert.Equal(7, sut.Execute());
        }

        [Fact]
        public void ExternalStaticTest()
        {
            var fake = new Fake<TestClass>();

            var sut = fake.Rewrite(f => f.GetHelperDynamicStaticValue());
            sut.Replace(() => HelperClass.DynamicStaticValue).Return(7);

            Assert.Equal(7, sut.Execute());
        }

        [Fact]
        public void FrameworkInstanceTest()
        {
            var fake = new Fake<TestClass>();

            const string cmd = "select * from Test";
            var sut = fake.Rewrite(f => f.GetFrameworkValue());
            sut.Replace((SqlCommand c) => c.CommandText).Return(cmd);

            Assert.Equal(cmd, sut.Execute());
        }

        [Fact]
        public void FrameworkStaticTest()
        {
            var fake = new Fake<TestClass>();

            var date = new DateTime(2016, 9, 25);
            var sut = fake.Rewrite(f => f.GetFrameworkStaticValue());
            sut.Replace(() => DateTime.Now).Return(date);

            Assert.Equal(date, sut.Execute());
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
