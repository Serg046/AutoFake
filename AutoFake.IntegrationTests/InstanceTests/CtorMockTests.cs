﻿using System.Data.SqlClient;
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

            fake.Rewrite(f => f.GetTestClass())
                .Replace(() => new TestClass()).Return(testClass);

            fake.Execute(tst => Assert.Equal(testClass, tst.GetTestClass()));
        }

        [Fact]
        public void ExternalInstanceTest()
        {
            var helperClass = new HelperClass();
            var fake = new Fake<TestClass>();

            fake.Rewrite(f => f.GetHelperClass())
                .Replace(() => new HelperClass()).Return(helperClass);

            fake.Execute(tst => Assert.Equal(helperClass, tst.GetHelperClass()));
        }

        [Fact]
        public void FrameworkTest()
        {
            var cmd = new SqlCommand();
            var fake = new Fake<TestClass>();

            fake.Rewrite(f => f.GetSqlCommand())
                .Replace(() => new SqlCommand()).Return(cmd);

            fake.Execute(tst => Assert.Equal(cmd, tst.GetSqlCommand()));
        }

        [Fact]
        public void MultipleArgumentsTest()
        {
            var reader = new StringReader("test");
            var fake = new Fake<TestClass>();
            
            fake.Rewrite(f => f.GetStringReader())
                .Replace(() => new StringReader("")).Return(reader);

            fake.Execute(tst => Assert.Equal(reader, tst.GetStringReader()));
        }

        [Fact]
        public void OverloadedCtorTest()
        {
            var fake = new Fake<TestClass>();

            var method = fake.Rewrite(f => f.GetOverloadCtorTestClass());
            method.Replace(() => new OverloadCtorTestClass()).Return(() => new OverloadCtorTestClass(6));
            method.Replace(() => new OverloadCtorTestClass(Arg.IsAny<int>())).Return(() => new OverloadCtorTestClass(7));

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
