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

            fake.Replace(() => new TestClass()).Return(() => testClass);
            fake.Rewrite(f => f.GetTestClass());

            fake.Execute(tst => Assert.Equal(testClass, tst.GetTestClass()));
        }

        [Fact]
        public void ExternalInstanceTest()
        {
            var helperClass = new HelperClass();
            var fake = new Fake<TestClass>();

            fake.Replace(() => new HelperClass()).Return(() => helperClass);
            fake.Rewrite(f => f.GetHelperClass());

            fake.Execute(tst => Assert.Equal(helperClass, tst.GetHelperClass()));
        }

        [Fact]
        public void FrameworkTest()
        {
            var cmd = new SqlCommand();
            var fake = new Fake<TestClass>();

            fake.Replace(() => new SqlCommand()).Return(() => cmd);
            fake.Rewrite(f => f.GetSqlCommand());

            fake.Execute(tst => Assert.Equal(cmd, tst.GetSqlCommand()));
        }

        [Fact]
        public void MultipleArgumentsTest()
        {
            var reader = new StringReader("test");
            var fake = new Fake<TestClass>();
            
            fake.Replace(() => new StringReader("")).Return(() => reader);
            fake.Rewrite(f => f.GetStringReader());

            fake.Execute(tst => Assert.Equal(reader, tst.GetStringReader()));
        }

        [Fact]
        public void OverloadedCtorTest()
        {
            var fake = new Fake<TestClass>();

            fake.Replace(() => new OverloadCtorTestClass()).Return(() => new OverloadCtorTestClass(6));
            fake.Replace(() => new OverloadCtorTestClass(Arg.DefaultOf<int>())).Return(() => new OverloadCtorTestClass(7));
            fake.Rewrite(f => f.GetOverloadCtorTestClass());

            fake.Execute(tst => Assert.Equal(7, tst.GetOverloadCtorTestClass().Value));
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
