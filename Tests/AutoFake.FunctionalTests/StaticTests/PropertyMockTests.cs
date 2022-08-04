using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace AutoFake.FunctionalTests.StaticTests
{
    public class PropertyMockTests
    {
        [Fact]
        public void OwnStaticTest()
        {
            var fake = new Fake(typeof(TestClass));

            var sut = fake.Rewrite(() => TestClass.GetDynamicStaticValue());
            sut.Replace(() => TestClass.DynamicStaticValue).Return(7);

            Assert.Equal(7, sut.Execute());
        }

        [Fact]
        public void ExternalStaticTest()
        {
            var fake = new Fake(typeof(TestClass));

            var sut = fake.Rewrite(() => TestClass.GetHelperDynamicStaticValue());
            sut.Replace(() => HelperClass.DynamicStaticValue).Return(7);

            Assert.Equal(7, sut.Execute());
        }

        [Fact]
        public void FrameworkInstanceTest()
        {
            var fake = new Fake(typeof(TestClass));

            const string cmd = "select * from Test";
            var sut = fake.Rewrite(() => TestClass.GetFrameworkValue());
            sut.Replace((SqlCommand c) => c.CommandText).Return(cmd);

            Assert.Equal(cmd, sut.Execute());
        }

        [Fact]
        public void FrameworkStaticTest()
        {
            var fake = new Fake(typeof(TestClass));

            var date = new DateTime(2016, 9, 25);
            var sut = fake.Rewrite(() => TestClass.GetFrameworkStaticValue());
            sut.Replace(() => DateTime.Now).Return(date);

            Assert.Equal(date, sut.Execute());
        }

        [Fact]
        public void GenericTest()
        {
	        var fake = new Fake(typeof(GenericTestClass<int>));

	        var sut = fake.Rewrite(() => GenericTestClass<int>.GetValue(0, "0"));
	        sut.Replace(() => GenericTestClass<int>.Pair).Return(new KeyValuePair<int, string>(1, "1"));

	        var actual = sut.Execute();
	        Assert.Equal(1, actual.Key);
	        Assert.Equal("1", actual.Value);
        }

        [Fact]
        public void WhenTest()
        {
            var fake = new Fake(typeof(WhenTestClass));

            var sut = fake.Rewrite(() => WhenTestClass.SomeMethod());
            sut.Replace(() => TestClass.DynamicStaticValue).Return(1)
                .When(x => x.Execute(() => WhenTestClass.Prop) == -1);
            sut.Replace(() => TestClass.DynamicStaticValue).Return(2)
                .When(x => x.Execute(() => WhenTestClass.Prop) == 1);

            sut.Execute().Should().Be(3);
        }

        [Fact]
        public void EnumerableMethodTest()
        {
            var fake = new Fake(typeof(EnumerableTestClass));

            var sut = fake.Rewrite(() => EnumerableTestClass.GetValue());
            sut.Replace(() => EnumerableTestClass.GetDynamicValue).Return(7);

            sut.Execute().Should().OnlyContain(i => i == 7);
        }

#if NETCOREAPP3_0
        [Fact]
        public async Task AsyncEnumerableMethodTest()
        {
            var fake = new Fake(typeof(AsyncEnumerableTestClass));

            var sut = fake.Rewrite(() => AsyncEnumerableTestClass.GetValue());
            sut.Replace(() => AsyncEnumerableTestClass.GetDynamicValue).Return(7);

            await foreach (var value in sut.Execute())
            {
                value.Should().Be(7);
            }
        }
#endif

#if NETCOREAPP3_0
        private static class AsyncEnumerableTestClass
        {
            public static int GetDynamicValue => 5;

            public static async IAsyncEnumerable<int> GetValue()
            {
                await Task.Yield();
                yield return GetDynamicValue;
            }
        }
#endif

        private static class EnumerableTestClass
        {
            public static int GetDynamicValue => 5;

            public static IEnumerable<int> GetValue()
            {
                yield return GetDynamicValue;
            }
        }

        private static class WhenTestClass
        {
            public static int Prop { get; set; }

            public static int SomeMethod()
            {
                Prop = -1;
                var x = TestClass.DynamicStaticValue;
                Prop = 1;
                return x + TestClass.DynamicStaticValue;
            }
        }

        private static class GenericTestClass<T>
        {
	        public static KeyValuePair<T, string> Pair => new KeyValuePair<T, string>(default, "test");
	        public static KeyValuePair<T, string> GetValue(T x, string y) => Pair;
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
