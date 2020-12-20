using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AutoFake.Exceptions;
using Xunit;

namespace AutoFake.IntegrationTests.InstanceTests
{
    public class MethodMockTests
    {
        [Fact]
        public void OwnInstanceTest()
        {
            var fake = new Fake<TestClass>();

            var sut = fake.Rewrite(f => f.GetDynamicValue());
            sut.Replace(t => t.DynamicValue()).Return(7);

            Assert.Equal(7, sut.Execute());
        }

        [Fact]
        public void ExternalInstanceTest()
        {
            var fake = new Fake<TestClass>();

            var sut = fake.Rewrite(f => f.GetHelperDynamicValue());
            sut.Replace((HelperClass h) => h.DynamicValue()).Return(7);
            
            Assert.Equal(7, sut.Execute());
        }

        [Fact]
        public void OwnStaticTest()
        {
            var fake = new Fake<TestClass>();

            var sut = fake.Rewrite(f => f.GetDynamicStaticValue());
            sut.Replace(() => TestClass.DynamicStaticValue()).Return(7);

            Assert.Equal(7, sut.Execute());
        }

        [Fact]
        public void ExternalStaticTest()
        {
            var fake = new Fake<TestClass>();

            var sut = fake.Rewrite(f => f.GetHelperDynamicStaticValue());
            sut.Replace(() => HelperClass.DynamicStaticValue()).Return(7);

            Assert.Equal(7, sut.Execute());
        }

        [Fact]
        public void FrameworkInstanceTest()
        {
            var fake = new Fake<TestClass>();

            var sut = fake.Rewrite(f => f.GetFrameworkValue());
            sut.Replace((SqlCommand c) => c.ExecuteScalar()).Return(7);

            Assert.Equal(7, sut.Execute());
        }

        [Fact]
        public void FrameworkStaticTest()
        {
            var fake = new Fake<TestClass>();

            var hashtable = new Hashtable {{ 1, 1 }};
            var sut = fake.Rewrite(f => f.GetFrameworkStaticValue());
            sut.Replace(() => CollectionsUtil.CreateCaseInsensitiveHashtable()).Return(hashtable);

            Assert.Equal(hashtable, sut.Execute());
        }

        [Fact]
        public void MultipleArgumentsTest()
        {
            var fake = new Fake<TestClass>();

            var sut = fake.Rewrite(t => t.GetValueByArguments(DateTime.UtcNow, TimeZoneInfo.Local));
            sut.Replace(() => TimeZoneInfo.ConvertTimeFromUtc(
                    Arg.IsAny<DateTime>(), Arg.IsAny<TimeZoneInfo>())).Return(DateTime.MinValue);

            Assert.Equal(DateTime.MinValue, sut.Execute());
        }

        [Fact]
        public void VoidMethodMockTest()
        {
            Assert.Throws<NotImplementedException>(() => new TestClass().UnsafeMethod());

            var fake = new Fake<TestClass>();

            var sut = fake.Rewrite(f => f.UnsafeMethod());
            sut.Remove(t => t.ThrowException());

            //no exception
            sut.Execute();
        }

        [Fact]
        public void OverloadedMethodTest()
        {
            var fake = new Fake<TestClass>();

            var sut = fake.Rewrite(f => f.GetDynValueByOveloadedMethodCalls());
            sut.Replace(t => t.DynamicValue()).Return(7);
            sut.Replace(t => t.DynamicValue(5)).Return(7);

            Assert.Equal(14, sut.Execute());
        }

        [Fact]
        public async Task AsyncInstanceMethodTest()
        {
            var fake = new Fake<AsyncTestClass>();

            var sut = fake.Rewrite(f => f.GetValueAsync());
            sut.Replace(a => a.GetDynamicValueAsync()).Return(Task.FromResult(7));

            Assert.Equal(7, await sut.Execute());
        }

        [Fact]
        public async Task AsyncStaticMethodTest()
        {
            var fake = new Fake<AsyncTestClass>();

            var sut = fake.Rewrite(f => f.GetStaticValueAsync());
            sut.Replace(() => AsyncTestClass.GetStaticDynamicValueAsync()).Return(Task.FromResult(7));

            Assert.Equal(7, await sut.Execute());
        }

        [Fact]
        public void ParamsMethodTest()
        {
            var fake = new Fake<ParamsTestClass>();
            var sut = fake.Rewrite(f => f.Test());
            sut.Replace(p => p.GetValue(1, 2, 3)).Return(-1);

            Assert.Equal(-1, sut.Execute());

            fake = new Fake<ParamsTestClass>();
            sut = fake.Rewrite(f => f.Test());
            sut.Replace(p => p.GetValue(Arg.Is(new[] { 1, 2, 3 }, new IntArrayComparer())))
                .Return(-1);

            Assert.Equal(-1, sut.Execute());
        }

        [Theory]
        [InlineData(5, false, true)]
        [InlineData(3, true, true)]
        [InlineData(3, false, true)]
        [InlineData(5, true, false)]
        public void LambdaArgumentTest(int day, bool isCorrectZone, bool throws)
        {
            var fake = new Fake<TestClass>();

            var date = new DateTime(2016, 11, day);
            var zone = isCorrectZone
                ? TimeZoneInfo.CreateCustomTimeZone("correct", TimeSpan.FromHours(6), "", "")
                : TimeZoneInfo.CreateCustomTimeZone("incorrect", TimeSpan.FromHours(-6), "", "");
            var sut = fake.Rewrite(f => f.GetValueByArguments(date, zone));
            sut.Verify(() => TimeZoneInfo.ConvertTimeFromUtc(Arg.Is<DateTime>(d => d > new DateTime(2016, 11, 04)),
                Arg.Is<TimeZoneInfo>(t => t.BaseUtcOffset.Hours > 0)));

            if (throws)
            {
                Assert.Throws<VerifyException>(() => sut.Execute());
            }
            else
            {
                sut.Execute();
            }
        }

        [Fact]
        public void RecursionTest()
        {
            var fake = new Fake<TestClass>();

            var sut = fake.Rewrite(f => f.GetRecursionValue(2));
            sut.Replace(f => f.GetRecursionValue(Arg.IsAny<int>())).Return(-1);

            Assert.Equal(-1, sut.Execute());
        }
        
        [Fact]
        public void GenericTest()
        {
	        var fake = new Fake<GenericTestClass<int>>();

	        var sut = fake.Rewrite(f => f.GetValue(0, "0"));
	        sut.Replace(s => s.GetValueImp(Arg.IsAny<int>(), "0")).Return(new KeyValuePair<int, string>(1, "1"));

	        var actual = sut.Execute();
	        Assert.Equal(1, actual.Key);
	        Assert.Equal("1", actual.Value);
        }

        private class GenericTestClass<T>
        {
            public KeyValuePair<T, T2> GetValueImp<T2>(T x, T2 y) => new KeyValuePair<T,T2>(x , y);
	        public KeyValuePair<T, T2> GetValue<T2>(T x, T2 y) => GetValueImp(x, y);
        }
        
        private class TestClass
        {
            public int DynamicValue() => 5;
            public int DynamicValue(int value) => value;
            public static int DynamicStaticValue() => 5;

            public void ThrowException()
            {
                throw new NotImplementedException();
            }

            public int GetDynamicValue()
            {
                Debug.WriteLine("Started");
                var value = DynamicValue();
                Debug.WriteLine("Finished");
                return value;
            }

            public int GetHelperDynamicValue()
            {
                Debug.WriteLine("Started");
                var helper = new HelperClass();
                var value = helper.DynamicValue();
                Debug.WriteLine("Finished");
                return value;
            }

            public int GetDynamicStaticValue()
            {
                Debug.WriteLine("Started");
                var value = DynamicStaticValue();
                Debug.WriteLine("Finished");
                return value;
            }

            public int GetHelperDynamicStaticValue()
            {
                Debug.WriteLine("Started");
                var value = HelperClass.DynamicStaticValue();
                Debug.WriteLine("Finished");
                return value;
            }

            public object GetFrameworkValue()
            {
                Debug.WriteLine("Started");
                var cmd = new SqlCommand();
                var value = cmd.ExecuteScalar();
                Debug.WriteLine("Finished");
                return value;
            }

            public Hashtable GetFrameworkStaticValue()
            {
                Debug.WriteLine("Started");
                var value = CollectionsUtil.CreateCaseInsensitiveHashtable();
                Debug.WriteLine("Finished");
                return value;
            }

            public DateTime GetValueByArguments(DateTime dateTime, TimeZoneInfo zone)
            {
                Debug.WriteLine("Started");
                var value = TimeZoneInfo.ConvertTimeFromUtc(dateTime, zone);
                Debug.WriteLine("Finished");
                return value;
            }

            public void UnsafeMethod()
            {
                Debug.WriteLine("Started");
                ThrowException();
                Debug.WriteLine("Finished");
            }

            public int GetDynValueByOveloadedMethodCalls()
            {
                Debug.WriteLine("Started");
                var value = DynamicValue() + DynamicValue(5);
                Debug.WriteLine("Finished");
                return value;
            }

            public int GetRecursionValue(int value)
            {
                if (value < 5)
                {
                    return GetRecursionValue(value + 1);
                }
                return value;
            }
        }

        private class HelperClass
        {
            public int DynamicValue() => 5;
            public static int DynamicStaticValue() => 5;
        }

        private class AsyncTestClass
        {
            public async Task<int> GetDynamicValueAsync()
            {
                await Task.Delay(1);
                var value = 5;
                await Task.Delay(1);
                return value;
            }

            public static async Task<int> GetStaticDynamicValueAsync()
            {
                await Task.Delay(1);
                var value = 5;
                await Task.Delay(1);
                return value;
            }

            public async Task<int> GetValueAsync() => await GetDynamicValueAsync();
            public async Task<int> GetStaticValueAsync() => await GetStaticDynamicValueAsync();
        }

        private class ParamsTestClass
        {
            public int GetValue(params int[] values) => values.Sum();

            public int Test()
            {
                Debug.WriteLine("Started");
                var value = GetValue(1, 2, 3);
                Debug.WriteLine("Finished");
                return value;
            }
        }

        private class IntArrayComparer : IEqualityComparer<int[]>
        {
            public bool Equals(int[] x, int[] y) => x.SequenceEqual(y);
            public int GetHashCode(int[] obj) => obj.GetHashCode();
        }
    }
}
