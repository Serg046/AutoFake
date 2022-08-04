using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using FluentAssertions;
using Xunit;

namespace AutoFake.FunctionalTests.StaticTests
{
    public class FieldMockTests
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

            var sr = new StringReader(string.Empty);
            var sut = fake.Rewrite(() => TestClass.GetFrameworkStaticValue());
            sut.Replace(() => TextReader.Null).Return(sr);

            var actual = sut.Execute();
            Assert.Equal(sr, actual);
            Assert.NotEqual(TextReader.Null, actual);
        }

        [Fact]
        public void StaticStructFieldByAddress()
        {
            var fake = new Fake(typeof(TestClass));

            const int value = 5;
            var data = new HelperStruct {Value = value};
            var sut = fake.Rewrite(() => TestClass.GetStaticStructValueByAddress());
            sut.Replace(() => TestClass.StaticStructValue).Return(data);

            Assert.Equal(value, sut.Execute());
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
        public void AnotherGenericTest()
        {
	        var fake = new Fake(typeof(TestClass));
	        const string stringValue = "testValue";
	        const int intValue = 7;

	        var sut = fake.Rewrite(() => TestClass.GetGenericValue());
	        sut.Replace((ValueTuple<string> tuple) => tuple.Item1).Return(stringValue);
	        sut.Replace((ValueTuple<object> tuple) => tuple.Item1).Return(intValue);

	        sut.Execute().Should().Be(stringValue + intValue);
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
        public void IEnumerableMethodTest()
        {
            var fake = new Fake(typeof(IEnumerableTestClass));

            var sut = fake.Rewrite(() => IEnumerableTestClass.GetValue());
            sut.Replace(() => IEnumerableTestClass.GetDynamicValue).Return(7);

            sut.Execute().Should().OnlyContain(i => i == 7);
        }

        private static class IEnumerableTestClass
        {
            public static int GetDynamicValue = 5;

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
            public static KeyValuePair<T, string> Pair = new KeyValuePair<T, string>(default, "test");
            public static KeyValuePair<T, string> GetValue(T x, string y) => Pair;
        }

#pragma warning disable 0649
        private static class TestClass
        {
            public static int DynamicStaticValue = 5;
            public static HelperStruct StaticStructValue;

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
                var value = cmd.CommandText;
                Debug.WriteLine("Finished");
                return value;
            }

            public static TextReader GetFrameworkStaticValue()
            {
                Debug.WriteLine("Started");
                var value = TextReader.Null;
                Debug.WriteLine("Finished");
                return value;
            }

            public static int GetStaticStructValueByAddress()
            {
                Debug.WriteLine("Started");
                var value = StaticStructValue.Value;
                Debug.WriteLine("Finished");
                return value;
            }

            public static string GetGenericValue()
            {
	            Debug.WriteLine("Started");
	            var tuple1 = new ValueTuple<string>();
	            var tuple2 = new ValueTuple<object>();
	            var value = tuple1.Item1 + tuple2.Item1;
	            Debug.WriteLine("Finished");
	            return value;
            }
        }

        private static class HelperClass
        {
            public static int DynamicStaticValue = 5;
        }

        public struct HelperStruct
        {
            public int Value;
        }
    }
}
