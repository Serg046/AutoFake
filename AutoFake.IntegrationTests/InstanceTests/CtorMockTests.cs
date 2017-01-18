using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace AutoFake.IntegrationTests.InstanceTests
{
    public class CtorMockTests
    {
        [Fact]
        public void OwnTest()
        {
            var testClass = new TestClass();
            var fake = new Fake<TestClass>();

            fake.Replace(() => new TestClass()).Returns(testClass);

            Assert.Equal(testClass, fake.Rewrite(f => f.GetTestClass()).Execute());
        }

        [Fact]
        public void ExternalInstanceTest()
        {
            var helperClass = new HelperClass();
            var fake = new Fake<TestClass>();

            fake.Replace(() => new HelperClass()).Returns(helperClass);

            Assert.Equal(helperClass, fake.Rewrite(f => f.GetHelperClass()).Execute());
        }

        [Fact]
        public void FrameworkTest()
        {
            var cmd = new SqlCommand();
            var fake = new Fake<TestClass>();

            fake.Replace(() => new SqlCommand()).Returns(cmd);

            Assert.Equal(cmd, fake.Rewrite(f => f.GetSqlCommand()).Execute());
        }

        [Fact]
        public void MultipleArgumentsTest()
        {
            var reader = new StringReader("test");
            var fake = new Fake<TestClass>();
            
            fake.Replace(() => new StringReader("")).Returns(reader);

            Assert.Equal(reader, fake.Rewrite(f => f.GetStringReader()).Execute());
        }

        [Fact]
        public void OverloadedCtorTest()
        {
            var fake = new Fake<TestClass>();

            fake.Replace(() => new OverloadCtorTestClass()).Returns(new OverloadCtorTestClass(5));
            fake.Replace(() => new OverloadCtorTestClass(Arg.DefaultOf<int>())).Returns(new OverloadCtorTestClass(7));

            Assert.Equal(7, fake.Rewrite(f => f.GetOverloadCtorTestClass()).Execute().Value);
        }

        private class TestClass
        {
            public TestClass GetTestClass()
            {
                Console.WriteLine("Started");
                var value = new TestClass();
                Console.WriteLine("Finished");
                return value;
            }

            public HelperClass GetHelperClass()
            {
                Console.WriteLine("Started");
                var helper = new HelperClass();
                Console.WriteLine("Finished");
                return helper;
            }

            public SqlCommand GetSqlCommand()
            {
                Console.WriteLine("Started");
                var cmd = new SqlCommand();
                Console.WriteLine("Finished");
                return cmd;
            }

            public StringReader GetStringReader()
            {
                Console.WriteLine("Started");
                var reader = new StringReader("");
                Console.WriteLine("Finished");
                return reader;
            }

            public OverloadCtorTestClass GetOverloadCtorTestClass()
            {
                Console.WriteLine("Started");
                var obj = new OverloadCtorTestClass(5);
                Console.WriteLine("Finished");
                return obj;
            }
        }

        private class HelperClass
        {

        }

        private class OverloadCtorTestClass
        {
            public OverloadCtorTestClass()
            {
            }

            public OverloadCtorTestClass(int arg)
            {
                Value = arg;
            }

            public int Value { get; }
        }
    }
}
