using System;
using System.Collections;
using System.Collections.Specialized;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AutoFake.Exceptions;
using Xunit;

namespace AutoFake.IntegrationTests.StaticTests
{
    public class MethodMockTests
    {
        [Fact]
        public void OwnStaticTest()
        {
            var fake = new Fake(typeof(TestClass));

            var sut = fake.Rewrite(() => TestClass.GetDynamicStaticValue());
            sut.Replace(() => TestClass.DynamicStaticValue()).Return(7);

            Assert.Equal(7, sut.Execute());
        }

        [Fact]
        public void ExternalStaticTest()
        {
            var fake = new Fake(typeof(TestClass));

            var sut = fake.Rewrite(() => TestClass.GetHelperDynamicStaticValue());
            sut.Replace(() => HelperClass.DynamicStaticValue()).Return(7);

            Assert.Equal(7, sut.Execute());
        }

        [Fact]
        public void FrameworkInstanceTest()
        {
            var fake = new Fake(typeof(TestClass));

            var sut = fake.Rewrite(() => TestClass.GetFrameworkValue());
            sut.Replace((SqlCommand c) => c.ExecuteScalar()).Return(7);

            Assert.Equal(7, sut.Execute());
        }

        [Fact]
        public void FrameworkStaticTest()
        {
            var fake = new Fake(typeof(TestClass));

            var hashtable = new Hashtable {{1, 1}};
            var sut = fake.Rewrite(() => TestClass.GetFrameworkStaticValue());
            sut.Replace(() => CollectionsUtil.CreateCaseInsensitiveHashtable()).Return(hashtable);

            Assert.Equal(hashtable, sut.Execute());
        }

        [Fact]
        public void MultipleArgumentsTest()
        {
            var fake = new Fake(typeof(TestClass));

            var sut = fake.Rewrite(() => TestClass.GetValueByArguments(DateTime.UtcNow, TimeZoneInfo.Local));
            sut.Replace(() => TimeZoneInfo.ConvertTimeFromUtc(
                    Arg.IsAny<DateTime>(), Arg.IsAny<TimeZoneInfo>())).Return(DateTime.MinValue);

            Assert.Equal(DateTime.MinValue, sut.Execute());
        }

        [Fact]
        public void VoidMethodMockTest()
        {
            Assert.Throws<NotImplementedException>(() => TestClass.UnsafeMethod());

            var fake = new Fake(typeof(TestClass));

            var sut = fake.Rewrite(() => TestClass.UnsafeMethod());
            sut.Remove(() => TestClass.ThrowException());

            //no exception
            sut.Execute();
        }

        [Fact]
        public void OverloadedMethodTest()
        {
            var fake = new Fake(typeof(TestClass));

            var sut = fake.Rewrite(() => TestClass.GetDynValueByOveloadedMethodCalls());
            sut.Replace(() => TestClass.DynamicStaticValue()).Return(7);
            sut.Replace(() => TestClass.DynamicStaticValue(5)).Return(7);

            Assert.Equal(14, sut.Execute());
        }

        [Fact]
        public async void AsyncStaticMethodTest()
        {
            var fake = new Fake(typeof(AsyncTestClass));

            var sut = fake.Rewrite(() => AsyncTestClass.GetStaticValueAsync());
            sut.Replace(() => AsyncTestClass.GetStaticDynamicValueAsync()).Return(Task.FromResult(7));

            Assert.Equal(7, await sut.Execute());
        }

        [Fact]
        public void ParamsMethodTest()
        {
            var fake = new Fake(typeof(ParamsTestClass));
            var sut = fake.Rewrite(() => ParamsTestClass.Test());
            sut.Replace(() => ParamsTestClass.GetValue(1, 2, 3)).Return(-1);

            Assert.Equal(-1, sut.Execute());

            fake = new Fake(typeof(ParamsTestClass));
            sut = fake.Rewrite(() => ParamsTestClass.Test());
            sut.Replace(() => ParamsTestClass.GetValue(new[] { 1, 2, 3 })).Return(-1);

            Assert.Equal(-1, sut.Execute());
        }

        [Theory]
        [InlineData(5, false, true)]
        [InlineData(3, true, true)]
        [InlineData(3, false, true)]
        [InlineData(5, true, false)]
        public void LambdaArgumentTest(int day, bool isCorrectZone, bool throws)
        {
            var fake = new Fake(typeof(TestClass));

            var date = new DateTime(2016, 11, day);
            var zone = isCorrectZone
                ? TimeZoneInfo.CreateCustomTimeZone("correct", TimeSpan.FromHours(6), "", "")
                : TimeZoneInfo.CreateCustomTimeZone("incorrect", TimeSpan.FromHours(-6), "", "");
            var sut = fake.Rewrite(() => TestClass.GetValueByArguments(date, zone));
            sut.Verify(() => TimeZoneInfo.ConvertTimeFromUtc(Arg.Is<DateTime>(d => d > new DateTime(2016, 11, 04)),
                Arg.Is<TimeZoneInfo>(t => t.BaseUtcOffset.Hours > 0))).CheckArguments();

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
            var fake = new Fake(typeof(TestClass));

            var sut = fake.Rewrite(() => TestClass.GetRecursionValue(2));
            sut.Replace(() => TestClass.GetRecursionValue(Arg.IsAny<int>())).Return(-1);

            Assert.Equal(-1, sut.Execute());
        }

        private static class TestClass
        {
            public static int DynamicStaticValue() => 5;
            public static int DynamicStaticValue(int value) => value;

            public static void ThrowException()
            {
                throw new NotImplementedException();
            }

            public static int GetDynamicStaticValue()
            {
                Debug.WriteLine("Started");
                var value = DynamicStaticValue();
                Debug.WriteLine("Finished");
                return value;
            }

            public static int GetHelperDynamicStaticValue()
            {
                Debug.WriteLine("Started");
                var value = HelperClass.DynamicStaticValue();
                Debug.WriteLine("Finished");
                return value;
            }

            public static object GetFrameworkValue()
            {
                Debug.WriteLine("Started");
                var cmd = new SqlCommand();
                var vaue = cmd.ExecuteScalar();
                Debug.WriteLine("Finished");
                return vaue;
            }

            public static Hashtable GetFrameworkStaticValue()
            {
                Debug.WriteLine("Started");
                var value = CollectionsUtil.CreateCaseInsensitiveHashtable();
                Debug.WriteLine("Finished");
                return value;
            }

            public static DateTime GetValueByArguments(DateTime dateTime, TimeZoneInfo zone)
            {
                Debug.WriteLine("Started");
                var value = TimeZoneInfo.ConvertTimeFromUtc(dateTime, zone);
                Debug.WriteLine("Finished");
                return value;
            }

            public static void UnsafeMethod()
            {
                Debug.WriteLine("Started");
                ThrowException();
                Debug.WriteLine("Finished");
            }

            public static int GetDynValueByOveloadedMethodCalls()
            {
                Debug.WriteLine("Started");
                var value = DynamicStaticValue() + DynamicStaticValue(5);
                Debug.WriteLine("Finished");
                return value;
            }

            public static int GetRecursionValue(int value)
            {
                if (value < 5)
                {
                    return GetRecursionValue(value + 1);
                }
                return value;
            }
        }

        private static class HelperClass
        {
            public static int DynamicStaticValue() => 5;
        }

        private static class AsyncTestClass
        {
            public static async Task<int> GetStaticDynamicValueAsync()
            {
                await Task.Delay(1);
                var value = 5;
                await Task.Delay(1);
                return value;
            }

            public static async Task<int> GetStaticValueAsync() => await GetStaticDynamicValueAsync();
        }

        private static class ParamsTestClass
        {
            public static int GetValue(params int[] values) => values.Sum();

            public static int Test()
            {
                Debug.WriteLine("Started");
                var value = GetValue();
                Debug.WriteLine("Finished");
                return value;
            }
        }
    }
}
