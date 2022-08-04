using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
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

        [Fact]
        public void RewriteCtorTest()
        {
	        var fake = new Fake<TestCtorClass>();

	        var sut = fake.Rewrite(f => new TestCtorClass());
	        sut.Replace(f => f.GetValue()).Return(5);

            Assert.Equal(5, fake.Execute(f => f.Value));
        }

        [Fact]
        public void WhenTest()
        {
            var fake = new Fake<WhenTestClass>();

            var sut = fake.Rewrite(f => f.SomeMethod());
            sut.Replace(() => new string(Arg.IsAny<char[]>())).Return("c")
                .When(x => x.Execute(f => f.Prop) == -1);
            sut.Replace(() => new string(Arg.IsAny<char[]>())).Return("d")
                .When(x => x.Execute(f => f.Prop) == 1);

            sut.Execute().Should().Be("cd");
        }

        [Fact]
        public void EnumerableMethodTest()
        {
            var fake = new Fake<EnumerableTestClass>();

            var sut = fake.Rewrite(f => f.GetValue());
            sut.Replace(() => new string(Arg.IsAny<char[]>())).Return("b");

            sut.Execute().Should().OnlyContain(i => i == "b");
        }

#if NETCOREAPP3_0
        [Fact]
        public async Task AsyncEnumerableMethodTest()
        {
            var fake = new Fake<AsyncEnumerableTestClass>();

            var sut = fake.Rewrite(f => f.GetValue());
            sut.Replace(() => new string(Arg.IsAny<char[]>())).Return("b");

            await foreach (var value in sut.Execute())
			{
                value.Should().Be("b");
			}
        }
#endif

#if NETCOREAPP3_0
        private class AsyncEnumerableTestClass
        {
            public int GetDynamicValue() => 5;

            public async IAsyncEnumerable<string> GetValue()
            {
                await Task.Yield();
                yield return new string(new[] { 'a' });
            }
        }
#endif

        private class EnumerableTestClass
        {
            public int GetDynamicValue() => 5;

            public IEnumerable<string> GetValue()
            {
                yield return new string(new[] { 'a' });
            }
        }

        private class WhenTestClass
        {
            public int Prop { get; set; }

            public string SomeMethod()
            {
                Prop = -1;
                var x = new string(new[] { 'a' });
                Prop = 1;
                return x + new string(new[] { 'b' });
            }
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

        private class TestCtorClass
        {
	        public TestCtorClass()
	        {
		        Value = GetValue();
	        }

            public int Value { get; }

	        public int GetValue() => throw new NotImplementedException();
        }
    }
}
