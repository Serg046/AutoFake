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

            fake.Rewrite(() => TestClass.GetDynamicStaticValue())
                .Replace(() => TestClass.DynamicStaticValue()).Return(() => 7);

            fake.Execute(tst => Assert.Equal(7, TestClass.GetDynamicStaticValue()));
        }

        [Fact]
        public void ExternalStaticTest()
        {
            var fake = new Fake(typeof(TestClass));

            fake.Rewrite(() => TestClass.GetHelperDynamicStaticValue())
                .Replace(() => HelperClass.DynamicStaticValue()).Return(() => 7);

            fake.Execute(tst => Assert.Equal(7, TestClass.GetHelperDynamicStaticValue()));
        }

        [Fact]
        public void FrameworkInstanceTest()
        {
            var fake = new Fake(typeof(TestClass));

            fake.Rewrite(() => TestClass.GetFrameworkValue())
                .Replace((SqlCommand c) => c.ExecuteScalar()).Return(() => 7);

            fake.Execute(tst => Assert.Equal(7, TestClass.GetFrameworkValue()));
        }

        [Fact]
        public void FrameworkStaticTest()
        {
            var fake = new Fake(typeof(TestClass));

            fake.Rewrite(() => TestClass.GetFrameworkStaticValue())
                .Replace(() => CollectionsUtil.CreateCaseInsensitiveHashtable()).Return(() => new Hashtable {{ 1, 1 }});

            fake.Execute((tst, prms) => Assert.Equal(prms.Single(), TestClass.GetFrameworkStaticValue()));
        }

        [Fact]
        public void MultipleArgumentsTest()
        {
            var fake = new Fake(typeof(TestClass));

            fake.Rewrite(() => TestClass.GetValueByArguments(DateTime.UtcNow, TimeZoneInfo.Local))
                .Replace(() => TimeZoneInfo.ConvertTimeFromUtc(
                    Arg.IsAny<DateTime>(), Arg.IsAny<TimeZoneInfo>())).Return(() => DateTime.MinValue);

            fake.Execute(tst => Assert.Equal(DateTime.MinValue,
                TestClass.GetValueByArguments(DateTime.UtcNow, TimeZoneInfo.Local)));
        }

        [Fact]
        public void VoidMethodMockTest()
        {
            Assert.Throws<NotImplementedException>(() => TestClass.UnsafeMethod());

            var fake = new Fake(typeof(TestClass));

            fake.Rewrite(() => TestClass.UnsafeMethod())
                .Remove(() => TestClass.ThrowException());

            //no exception
            fake.Execute(tst => TestClass.UnsafeMethod());
        }

        [Fact]
        public void OverloadedMethodTest()
        {
            var fake = new Fake(typeof(TestClass));

            var method = fake.Rewrite(() => TestClass.GetDynValueByOveloadedMethodCalls());
            method.Replace(() => TestClass.DynamicStaticValue()).Return(() => 7);
            method.Replace(() => TestClass.DynamicStaticValue(5)).Return(() => 7);

            fake.Execute(tst => Assert.Equal(14, TestClass.GetDynValueByOveloadedMethodCalls()));
        }

        [Fact]
        public async void AsyncStaticMethodTest()
        {
            var fake = new Fake(typeof(AsyncTestClass));

            fake.Rewrite(() => AsyncTestClass.GetStaticValueAsync())
                .Replace(() => AsyncTestClass.GetStaticDynamicValueAsync()).Return(() => Task.FromResult(7));

            await fake.ExecuteAsync(async tst =>
                Assert.Equal(7, await AsyncTestClass.GetStaticValueAsync()));
        }

        [Fact]
        public void ParamsMethodTest()
        {
            var fake = new Fake(typeof(ParamsTestClass));
            fake.Rewrite(() => ParamsTestClass.Test())
                .Replace(() => ParamsTestClass.GetValue(1, 2, 3)).Return(() => -1);

            fake.Execute(tst => Assert.Equal(-1, ParamsTestClass.Test()));

            fake = new Fake(typeof(ParamsTestClass));
            fake.Rewrite(() => ParamsTestClass.Test())
                .Replace(() => ParamsTestClass.GetValue(new[] { 1, 2, 3 })).Return(() => -1);

            fake.Execute(tst => Assert.Equal(-1, ParamsTestClass.Test()));
        }

        [Fact]
        public void LambdaArgumentTest()
        {
            var fake = new Fake(typeof(TestClass));

            fake.Rewrite(() => TestClass.GetValueByArguments(Arg.IsAny<DateTime>(), Arg.IsAny<TimeZoneInfo>()))
                .Verify(() => TimeZoneInfo.ConvertTimeFromUtc(Arg.Is<DateTime>(d => d > new DateTime(2016, 11, 04)),
                    Arg.Is<TimeZoneInfo>(t => t.BaseUtcOffset.Hours > 0))).CheckArguments();

            fake.Execute(tst =>
            {
                var correctZone = TimeZoneInfo.CreateCustomTimeZone("correct", TimeSpan.FromHours(6), "", "");
                var incorrectZone = TimeZoneInfo.CreateCustomTimeZone("incorrect", TimeSpan.FromHours(-6), "", "");
                Assert.Throws<VerifyException>(() => TestClass.GetValueByArguments(new DateTime(2016, 11, 05), incorrectZone));
                Assert.Throws<VerifyException>(() => TestClass.GetValueByArguments(new DateTime(2016, 11, 03), correctZone));
                Assert.Throws<VerifyException>(() => TestClass.GetValueByArguments(new DateTime(2016, 11, 03), incorrectZone));
                TestClass.GetValueByArguments(new DateTime(2016, 11, 05), correctZone);
            });
        }

        [Fact]
        public void RecursionTest()
        {
            var fake = new Fake(typeof(TestClass));

            fake.Rewrite(() => TestClass.GetRecursionValue(2))
                .Replace(() => TestClass.GetRecursionValue(Arg.IsAny<int>())).Return(() => -1);

            fake.Execute(tst => Assert.Equal(-1, TestClass.GetRecursionValue(2)));
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
