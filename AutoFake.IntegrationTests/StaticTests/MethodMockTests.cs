﻿using System;
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

            fake.Replace(() => TestClass.DynamicStaticValue()).Returns(7);

            Assert.Equal(7, fake.Rewrite(() => TestClass.GetDynamicStaticValue()).Execute());
        }

        [Fact]
        public void ExternalStaticTest()
        {
            var fake = new Fake(typeof(TestClass));

            fake.Replace(() => HelperClass.DynamicStaticValue()).Returns(7);

            Assert.Equal(7, fake.Rewrite(() => TestClass.GetHelperDynamicStaticValue()).Execute());
        }

        [Fact]
        public void FrameworkInstanceTest()
        {
            var fake = new Fake(typeof(TestClass));

            fake.Replace((SqlCommand c) => c.ExecuteScalar()).Returns(7);

            Assert.Equal(7, fake.Rewrite(() => TestClass.GetFrameworkValue()).Execute());
        }

        [Fact]
        public void FrameworkStaticTest()
        {
            var fake = new Fake(typeof(TestClass));

            var collection = new Hashtable();
            collection.Add(1, 1);
            fake.Replace(() => CollectionsUtil.CreateCaseInsensitiveHashtable()).Returns(collection);

            Assert.Equal(collection, fake.Rewrite(() => TestClass.GetFrameworkStaticValue()).Execute());
        }

        [Fact]
        public void MultipleArgumentsTest()
        {
            var fake = new Fake(typeof(TestClass));

            var date = DateTime.UtcNow;
            var zone = TimeZoneInfo.Local;
            fake.Replace(() => TimeZoneInfo.ConvertTimeFromUtc(date, zone)).Returns(DateTime.MinValue);

            Assert.Equal(DateTime.MinValue, fake.Rewrite(() => TestClass.GetValueByArguments(date, zone)).Execute());
        }

        [Fact]
        public void VoidMethodMockTest()
        {
            Assert.Throws<NotImplementedException>(() => TestClass.UnsafeMethod());

            var fake = new Fake(typeof(TestClass));

            fake.Replace(() => TestClass.ThrowException());

            fake.Rewrite(() => TestClass.UnsafeMethod()).Execute();
        }

        [Fact]
        public void OverloadedMethodTest()
        {
            var fake = new Fake(typeof(TestClass));

            fake.Replace(() => TestClass.DynamicStaticValue()).Returns(7);
            fake.Replace(() => TestClass.DynamicStaticValue(5)).Returns(7);

            Assert.Equal(14, fake.Rewrite(() => TestClass.GetDynValueByOveloadedMethodCalls()).Execute());
        }

        [Fact]
        public async void AsyncStaticMethodTest()
        {
            var fake = new Fake(typeof(AsyncTestClass));

            fake.Replace(() => AsyncTestClass.GetStaticDynamicValueAsync()).Returns(Task.FromResult(7));

            Assert.Equal(7, await fake.Rewrite(() => AsyncTestClass.GetStaticValueAsync()).Execute());
        }

        [Fact]
        public void ParamsMethodTest()
        {
            var fake = new Fake(typeof(ParamsTestClass));

            fake.Replace(() => ParamsTestClass.GetValue(1, 2, 3)).Returns(-1);

            Assert.Equal(-1, fake.Rewrite(() => ParamsTestClass.Test()).Execute());

            fake = new Fake(typeof(ParamsTestClass));
            var values = new[] { 1, 2, 3 };
            fake.Replace(() => ParamsTestClass.GetValue(values)).Returns(-1);

            Assert.Equal(-1, fake.Rewrite(() => ParamsTestClass.Test()).Execute());
        }

        [Fact]
        public void LambdaArgumentTest()
        {
            var asserDate = new DateTime(2016, 11, 04);
            var fake = new Fake(typeof(TestClass));

            fake.Verify(() => TimeZoneInfo.ConvertTimeFromUtc(Arg.Is<DateTime>(d => d > asserDate),
                Arg.Is<TimeZoneInfo>(t => !Equals(t, TimeZoneInfo.Local)))).CheckArguments();

            fake.Rewrite(() => TestClass.GetValueByArguments(Arg.DefaultOf<DateTime>(), Arg.DefaultOf<TimeZoneInfo>()));

            Assert.Throws<VerifiableException>(() => fake.Execute(() => TestClass.GetValueByArguments(new DateTime(2016, 11, 05), TimeZoneInfo.Local)));
            Assert.Throws<VerifiableException>(() => fake.Execute(() => TestClass.GetValueByArguments(new DateTime(2016, 11, 03), TimeZoneInfo.Utc)));
            Assert.Throws<VerifiableException>(() => fake.Execute(() => TestClass.GetValueByArguments(new DateTime(2016, 11, 03), TimeZoneInfo.Local)));
            fake.Execute(() => TestClass.GetValueByArguments(new DateTime(2016, 11, 05), TimeZoneInfo.Utc));
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