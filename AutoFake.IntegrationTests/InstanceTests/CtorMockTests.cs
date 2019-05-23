using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
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

            fake.Replace(() => new TestClass()).Returns(() => testClass);
            fake.Rewrite(f => f.GetTestClass());

            fake.Execute2(tst => Assert.Equal(testClass, tst.GetTestClass()));
        }

        [Fact]
        public void ExternalInstanceTest()
        {
            var helperClass = new HelperClass();
            var fake = new Fake<TestClass>();

            fake.Replace(() => new HelperClass()).Returns(() => helperClass);
            fake.Rewrite(f => f.GetHelperClass());

            fake.Execute2(tst => Assert.Equal(helperClass, tst.GetHelperClass()));
        }

        [Fact]
        public void FrameworkTest()
        {
            var cmd = new SqlCommand();
            var fake = new Fake<TestClass>();

            fake.Replace(() => new SqlCommand()).Returns(() => cmd);
            fake.Rewrite(f => f.GetSqlCommand());

            fake.Execute2(tst => Assert.Equal(cmd, tst.GetSqlCommand()));
        }

        [Fact]
        public void MultipleArgumentsTest()
        {
            var reader = new StringReader("test");
            var fake = new Fake<TestClass>();
            
            fake.Replace(() => new StringReader("")).Returns(() => reader);
            fake.Rewrite(f => f.GetStringReader());

            fake.Execute2(tst => Assert.Equal(reader, tst.GetStringReader()));
        }

        [Fact]
        public void OverloadedCtorTest()
        {
            var fake = new Fake<TestClass>();

            fake.Replace(() => new OverloadCtorTestClass()).Returns(() => new OverloadCtorTestClass(6));
            fake.Replace(() => new OverloadCtorTestClass(Arg.DefaultOf<int>())).Returns(() => new OverloadCtorTestClass(7));
            fake.Rewrite(f => f.GetOverloadCtorTestClass());

            fake.Execute2(tst => Assert.Equal(7, tst.GetOverloadCtorTestClass().Value));
        }

        private class TestClass
        {
            public TestClass GetTestClass()
            {
                Debug.WriteLine("Started");
                var value = new TestClass();
                Debug.WriteLine("Finished");
                return value;
            }

            public HelperClass GetHelperClass()
            {
                Debug.WriteLine("Started");
                var helper = new HelperClass();
                Debug.WriteLine("Finished");
                return helper;
            }

            public SqlCommand GetSqlCommand()
            {
                Debug.WriteLine("Started");
                var cmd = new SqlCommand();
                Debug.WriteLine("Finished");
                return cmd;
            }

            public StringReader GetStringReader()
            {
                Debug.WriteLine("Started");
                var reader = new StringReader("");
                Debug.WriteLine("Finished");
                return reader;
            }

            public OverloadCtorTestClass GetOverloadCtorTestClass()
            {
                Debug.WriteLine("Started");
                var obj = new OverloadCtorTestClass(5);
                Debug.WriteLine("Finished");
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
