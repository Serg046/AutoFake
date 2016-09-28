using System;
using System.Collections;
using System.Collections.Specialized;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace AutoFake.IntegrationTests.InstanceTests
{
    public class MethodMockTests
    {
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
                var vaue = cmd.ExecuteScalar();
                Debug.WriteLine("Finished");
                return vaue;
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
            public static async Task<int> GetStaticValueAsync() => await GetStaticDynamicValueAsync();
        }

        [Fact]
        public void OwnInstanceTest()
        {
            var fake = new Fake<TestClass>();

            fake.Replace((TestClass t) => t.DynamicValue()).Returns(7);

            Assert.Equal(7, fake.Execute(f => f.GetDynamicValue()));
        }

        [Fact]
        public void ExternalInstanceTest()
        {
            var fake = new Fake<TestClass>();

            fake.Replace((HelperClass h) => h.DynamicValue()).Returns(7);

            Assert.Equal(7, fake.Execute(f => f.GetHelperDynamicValue()));
        }

        [Fact]
        public void OwnStaticTest()
        {
            var fake = new Fake<TestClass>();

            fake.Replace(() => TestClass.DynamicStaticValue()).Returns(7);

            Assert.Equal(7, fake.Execute(f => f.GetDynamicStaticValue()));
        }

        [Fact]
        public void ExternalStaticTest()
        {
            var fake = new Fake<TestClass>();

            fake.Replace(() => HelperClass.DynamicStaticValue()).Returns(7);

            Assert.Equal(7, fake.Execute(f => f.GetHelperDynamicStaticValue()));
        }

        [Fact]
        public void FrameworkInstanceTest()
        {
            var fake = new Fake<TestClass>();

            fake.Replace((SqlCommand c) => c.ExecuteScalar()).Returns(7);

            Assert.Equal(7, fake.Execute(f => f.GetFrameworkValue()));
        }

        [Fact]
        public void FrameworkStaticTest()
        {
            var fake = new Fake<TestClass>();

            var collection = new Hashtable();
            collection.Add(1, 1);
            fake.Replace(() => CollectionsUtil.CreateCaseInsensitiveHashtable()).Returns(collection);

            Assert.Equal(collection, fake.Execute(f => f.GetFrameworkStaticValue()));
        }

        [Fact]
        public void MultipleArgumentsTest()
        {
            var fake = new Fake<TestClass>();

            var date = DateTime.UtcNow;
            var zone = TimeZoneInfo.Local;
            fake.Replace(() => TimeZoneInfo.ConvertTimeFromUtc(date, zone)).Returns(DateTime.MinValue);

            Assert.Equal(DateTime.MinValue, fake.Execute(t => t.GetValueByArguments(date, zone)));
        }

        [Fact]
        public void VoidMethodMockTest()
        {
            Assert.Throws<NotImplementedException>(() => new TestClass().UnsafeMethod());

            var fake = new Fake<TestClass>();

            fake.Replace((TestClass t) => t.ThrowException());

            fake.Execute(f => f.UnsafeMethod());
        }

        [Fact]
        public void OverloadedMethodTest()
        {
            var fake = new Fake<TestClass>();

            fake.Replace((TestClass t) => t.DynamicValue()).Returns(7);
            fake.Replace((TestClass t) => t.DynamicValue(5)).Returns(7);

            Assert.Equal(14, fake.Execute(f => f.GetDynValueByOveloadedMethodCalls()));
        }

        [Fact]
        public async void AsyncInstanceMethodTest()
        {
            var fake = new Fake<AsyncTestClass>();

            fake.Replace((AsyncTestClass a) => a.GetDynamicValueAsync()).Returns(Task.FromResult(7));

            Assert.Equal(7, await fake.Execute(f => f.GetValueAsync()));
        }

        [Fact]
        public async void AsyncStaticMethodTest()
        {
            var fake = new Fake<AsyncTestClass>();

            fake.Replace(() => AsyncTestClass.GetStaticDynamicValueAsync()).Returns(Task.FromResult(7));

            Assert.Equal(7, await fake.Execute(() => AsyncTestClass.GetStaticValueAsync()));
        }
    }
}
