using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using Xunit;

namespace AutoFake.FunctionalTests.InstanceTests
{
    public class CtorMockTests
    {
        [Fact]
        public void OwnTest()
        {
            var testClass = new TestClass();
            var fake = new Fake<TestClass>();

            var sut = fake.Rewrite(f => f.GetTestClass());
            sut.Replace(() => new TestClass()).Return(testClass);

            Assert.Equal(testClass, sut.Execute());
        }

        [Fact]
        public void ExternalInstanceTest()
        {
            var helperClass = new HelperClass();
            var fake = new Fake<TestClass>();

            var sut = fake.Rewrite(f => f.GetHelperClass());
            sut.Replace(() => new HelperClass()).Return(helperClass);

            Assert.Equal(helperClass, sut.Execute());
        }

        [Fact]
        public void FrameworkTest()
        {
            var cmd = new SqlCommand();
            var fake = new Fake<TestClass>();

            var sut = fake.Rewrite(f => f.GetSqlCommand());
            sut.Replace(() => new SqlCommand()).Return(cmd);

            Assert.Equal(cmd, sut.Execute());
        }

        [Fact]
        public void MultipleArgumentsTest()
        {
            var reader = new StringReader("test");
            var fake = new Fake<TestClass>();

            var sut = fake.Rewrite(f => f.GetStringReader());
            sut.Replace(() => new StringReader("")).Return(reader);

            Assert.Equal(reader, sut.Execute());
        }

        [Fact]
        public void OverloadedCtorTest()
        {
            var fake = new Fake<TestClass>();

            var sut = fake.Rewrite(f => f.GetOverloadCtorTestClass());
            sut.Replace(() => new OverloadCtorTestClass()).Return(new OverloadCtorTestClass(6));
            sut.Replace(() => new OverloadCtorTestClass(Arg.IsAny<int>())).Return(new OverloadCtorTestClass(7));

            Assert.Equal(7, sut.Execute().Value);
        }

        [Fact]
        public void GenericTest()
        {
	        var fake = new Fake<GenericTestClass<int>>();

	        var sut = fake.Rewrite(f => f.GetValue(0, "0"));
	        sut.Replace(s => new KeyValuePair<int, string>(Arg.IsAny<int>(), Arg.IsAny<string>()))
		        .Return(new KeyValuePair<int, string>(1, "1"));

	        var actual = sut.Execute();
	        Assert.Equal(1, actual.Key);
	        Assert.Equal("1", actual.Value);
        }

        private class GenericTestClass<T>
        {
	        public KeyValuePair<T, T2> GetValue<T2>(T x, T2 y) => new KeyValuePair<T, T2>(x, y);
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
