using System;
using System.Collections;
using System.Collections.Specialized;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace AutoFake.IntegrationTests.StaticTests
{
    public class MethodMockTests
    {
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

        [Fact]
        public void OwnStaticTest()
        {
            var fake = new Fake(typeof(TestClass));

            fake.Replace(() => TestClass.DynamicStaticValue()).Returns(7);

            Assert.Equal(7, fake.Execute(() => TestClass.GetDynamicStaticValue()));
        }

        [Fact]
        public void ExternalStaticTest()
        {
            var fake = new Fake(typeof(TestClass));

            fake.Replace(() => HelperClass.DynamicStaticValue()).Returns(7);

            Assert.Equal(7, fake.Execute(() => TestClass.GetHelperDynamicStaticValue()));
        }

        [Fact]
        public void FrameworkInstanceTest()
        {
            var fake = new Fake(typeof(TestClass));

            fake.Replace((SqlCommand c) => c.ExecuteScalar()).Returns(7);

            Assert.Equal(7, fake.Execute(() => TestClass.GetFrameworkValue()));
        }

        [Fact]
        public void FrameworkStaticTest()
        {
            var fake = new Fake(typeof(TestClass));

            var collection = new Hashtable();
            collection.Add(1, 1);
            fake.Replace(() => CollectionsUtil.CreateCaseInsensitiveHashtable()).Returns(collection);

            Assert.Equal(collection, fake.Execute(() => TestClass.GetFrameworkStaticValue()));
        }

        [Fact]
        public void MultipleArgumentsTest()
        {
            var fake = new Fake(typeof(TestClass));

            var date = DateTime.UtcNow;
            var zone = TimeZoneInfo.Local;
            fake.Replace(() => TimeZoneInfo.ConvertTimeFromUtc(date, zone)).Returns(DateTime.MinValue);

            Assert.Equal(DateTime.MinValue, fake.Execute(() => TestClass.GetValueByArguments(date, zone)));
        }

        [Fact]
        public void VoidMethodMockTest()
        {
            Assert.Throws<NotImplementedException>(() => TestClass.UnsafeMethod());

            var fake = new Fake(typeof(TestClass));

            fake.Replace(() => TestClass.ThrowException());

            fake.Execute(() => TestClass.UnsafeMethod());
        }

        [Fact]
        public void OverloadedMethodTest()
        {
            var fake = new Fake(typeof(TestClass));

            fake.Replace(() => TestClass.DynamicStaticValue()).Returns(7);
            fake.Replace(() => TestClass.DynamicStaticValue(5)).Returns(7);

            Assert.Equal(14, fake.Execute(() => TestClass.GetDynValueByOveloadedMethodCalls()));
        }

        [Fact]
        public async void AsyncStaticMethodTest()
        {
            var fake = new Fake(typeof(AsyncTestClass));

            fake.Replace(() => AsyncTestClass.GetStaticDynamicValueAsync()).Returns(Task.FromResult(7));

            Assert.Equal(7, await fake.Execute(() => AsyncTestClass.GetStaticValueAsync()));
        }
    }
}
