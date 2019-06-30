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

            fake.Replace(() => TestClass.DynamicStaticValue()).Returns(() => 7);
            fake.Rewrite(() => TestClass.GetDynamicStaticValue());

            fake.Execute(tst => Assert.Equal(7, tst.Execute(() => TestClass.GetDynamicStaticValue())));
        }

        [Fact]
        public void ExternalStaticTest()
        {
            var fake = new Fake(typeof(TestClass));

            fake.Replace(() => HelperClass.DynamicStaticValue()).Returns(() => 7);
            fake.Rewrite(() => TestClass.GetHelperDynamicStaticValue());

            fake.Execute(tst => Assert.Equal(7, tst.Execute(() => TestClass.GetHelperDynamicStaticValue())));
        }

        [Fact]
        public void FrameworkInstanceTest()
        {
            var fake = new Fake(typeof(TestClass));

            fake.Replace((SqlCommand c) => c.ExecuteScalar()).Returns(() => 7);
            fake.Rewrite(() => TestClass.GetFrameworkValue());

            fake.Execute(tst => Assert.Equal(7, tst.Execute(() => TestClass.GetFrameworkValue())));
        }

        [Fact]
        public void FrameworkStaticTest()
        {
            var fake = new Fake(typeof(TestClass));

            fake.Replace(() => CollectionsUtil.CreateCaseInsensitiveHashtable()).Returns(() => new Hashtable {{1, 1}});
            fake.Rewrite(() => TestClass.GetFrameworkStaticValue());

            fake.Execute((tst, prms) => Assert.Equal(prms.Single(), tst.Execute(() => TestClass.GetFrameworkStaticValue())));
        }

        [Fact]
        public void MultipleArgumentsTest()
        {
            var fake = new Fake(typeof(TestClass));

            fake.Replace(() => TimeZoneInfo.ConvertTimeFromUtc(Arg.DefaultOf<DateTime>(), Arg.DefaultOf<TimeZoneInfo>())).Returns(() => DateTime.MinValue);
            fake.Rewrite(() => TestClass.GetValueByArguments(DateTime.UtcNow, TimeZoneInfo.Local));

            fake.Execute(tst => Assert.Equal(DateTime.MinValue,
                tst.Execute(() => TestClass.GetValueByArguments(DateTime.UtcNow, TimeZoneInfo.Local))));
        }

        [Fact]
        public void VoidMethodMockTest()
        {
            Assert.Throws<NotImplementedException>(() => TestClass.UnsafeMethod());

            var fake = new Fake(typeof(TestClass));

            fake.Replace(() => TestClass.ThrowException());
            fake.Rewrite(() => TestClass.UnsafeMethod());

            //no exception
            fake.Execute(tst => tst.Execute(() => TestClass.UnsafeMethod()));
        }

        [Fact]
        public void OverloadedMethodTest()
        {
            var fake = new Fake(typeof(TestClass));

            fake.Replace(() => TestClass.DynamicStaticValue()).Returns(() => 7);
            fake.Replace(() => TestClass.DynamicStaticValue(5)).Returns(() => 7);
            fake.Rewrite(() => TestClass.GetDynValueByOveloadedMethodCalls());

            fake.Execute(tst => Assert.Equal(14, tst.Execute(() => TestClass.GetDynValueByOveloadedMethodCalls())));
        }

        [Fact]
        public async void AsyncStaticMethodTest()
        {
            var fake = new Fake(typeof(AsyncTestClass));

            fake.Replace(() => AsyncTestClass.GetStaticDynamicValueAsync()).Returns(() => Task.FromResult(7));
            fake.Rewrite(() => AsyncTestClass.GetStaticValueAsync());

            await fake.ExecuteAsync(async tst =>
                Assert.Equal(7, await tst.Execute(() => AsyncTestClass.GetStaticValueAsync())));
        }

        [Fact]
        public void ParamsMethodTest()
        {
            var fake = new Fake(typeof(ParamsTestClass));
            fake.Replace(() => ParamsTestClass.GetValue(1, 2, 3)).Returns(() => -1);
            fake.Rewrite(() => ParamsTestClass.Test());

            fake.Execute(tst => Assert.Equal(-1, tst.Execute(() => ParamsTestClass.Test())));

            fake = new Fake(typeof(ParamsTestClass));
            fake.Replace(() => ParamsTestClass.GetValue(new[] { 1, 2, 3 })).Returns(() => -1);
            fake.Rewrite(() => ParamsTestClass.Test());

            fake.Execute(tst => Assert.Equal(-1, tst.Execute(() => ParamsTestClass.Test())));
        }

        [Fact]
        public void LambdaArgumentTest()
        {
            var fake = new Fake(typeof(TestClass));

            fake.Verify(() => TimeZoneInfo.ConvertTimeFromUtc(Arg.Is<DateTime>(d => d > new DateTime(2016, 11, 04)),
                Arg.Is<TimeZoneInfo>(t => t.BaseUtcOffset.Hours > 0))).CheckArguments();
            fake.Rewrite(() => TestClass.GetValueByArguments(Arg.DefaultOf<DateTime>(), Arg.DefaultOf<TimeZoneInfo>()));

            fake.Execute(tst =>
            {
                var correctZone = TimeZoneInfo.CreateCustomTimeZone("correct", TimeSpan.FromHours(6), "", "");
                var incorrectZone = TimeZoneInfo.CreateCustomTimeZone("incorrect", TimeSpan.FromHours(-6), "", "");
                Assert.Throws<VerifiableException>(() => tst.Execute(() => TestClass.GetValueByArguments(new DateTime(2016, 11, 05), incorrectZone)));
                Assert.Throws<VerifiableException>(() => tst.Execute(() => TestClass.GetValueByArguments(new DateTime(2016, 11, 03), correctZone)));
                Assert.Throws<VerifiableException>(() => tst.Execute(() => TestClass.GetValueByArguments(new DateTime(2016, 11, 03), incorrectZone)));
                tst.Execute(() => TestClass.GetValueByArguments(new DateTime(2016, 11, 05), correctZone));
            });
        }

        [Fact]
        public void RecursionTest()
        {
            var fake = new Fake(typeof(TestClass));

            fake.Replace(() => TestClass.GetRecursionValue(Arg.DefaultOf<int>())).Returns(() => -1);
            fake.Rewrite(() => TestClass.GetRecursionValue(2));

            fake.Execute(tst => Assert.Equal(-1, tst.Execute(() => TestClass.GetRecursionValue(2))));
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
