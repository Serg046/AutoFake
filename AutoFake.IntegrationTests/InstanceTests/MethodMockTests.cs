using System;
using System.Collections;
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

            fake.Replace(t => t.DynamicValue()).Returns(7);

            Assert.Equal(7, fake.Rewrite(f => f.GetDynamicValue()).Execute());
        }

        [Fact]
        public void ExternalInstanceTest()
        {
            var fake = new Fake<TestClass>();

            fake.Replace((HelperClass h) => h.DynamicValue()).Returns(7);

            Assert.Equal(7, fake.Rewrite(f => f.GetHelperDynamicValue()).Execute());
        }

        [Fact]
        public void OwnStaticTest()
        {
            var fake = new Fake<TestClass>();

            fake.Replace(() => TestClass.DynamicStaticValue()).Returns(7);

            Assert.Equal(7, fake.Rewrite(f => f.GetDynamicStaticValue()).Execute());
        }

        [Fact]
        public void ExternalStaticTest()
        {
            var fake = new Fake<TestClass>();

            fake.Replace(() => HelperClass.DynamicStaticValue()).Returns(7);

            Assert.Equal(7, fake.Rewrite(f => f.GetHelperDynamicStaticValue()).Execute());
        }

        [Fact]
        public void FrameworkInstanceTest()
        {
            var fake = new Fake<TestClass>();

            fake.Replace((SqlCommand c) => c.ExecuteScalar()).Returns(7);

            Assert.Equal(7, fake.Rewrite(f => f.GetFrameworkValue()).Execute());
        }

        [Fact]
        public void FrameworkStaticTest()
        {
            var fake = new Fake<TestClass>();

            var collection = new Hashtable();
            collection.Add(1, 1);
            fake.Replace(() => CollectionsUtil.CreateCaseInsensitiveHashtable()).Returns(collection);

            Assert.Equal(collection, fake.Rewrite(f => f.GetFrameworkStaticValue()).Execute());
        }

        [Fact]
        public void MultipleArgumentsTest()
        {
            var fake = new Fake<TestClass>();

            var date = DateTime.UtcNow;
            var zone = TimeZoneInfo.Local;
            fake.Replace(() => TimeZoneInfo.ConvertTimeFromUtc(date, zone)).Returns(DateTime.MinValue);

            Assert.Equal(DateTime.MinValue, fake.Rewrite(t => t.GetValueByArguments(date, zone)).Execute());
        }

        [Fact]
        public void VoidMethodMockTest()
        {
            Assert.Throws<NotImplementedException>(() => new TestClass().UnsafeMethod());

            var fake = new Fake<TestClass>();

            fake.Replace(t => t.ThrowException());

            fake.Rewrite(f => f.UnsafeMethod()).Execute();
        }

        [Fact]
        public void OverloadedMethodTest()
        {
            var fake = new Fake<TestClass>();

            fake.Replace(t => t.DynamicValue()).Returns(7);
            fake.Replace(t => t.DynamicValue(5)).Returns(7);

            Assert.Equal(14, fake.Rewrite(f => f.GetDynValueByOveloadedMethodCalls()).Execute());
        }

        [Fact]
        public async void AsyncInstanceMethodTest()
        {
            var fake = new Fake<AsyncTestClass>();

            fake.Replace(a => a.GetDynamicValueAsync()).Returns(Task.FromResult(7));

            Assert.Equal(7, await fake.Rewrite(f => f.GetValueAsync()).Execute());
        }

        [Fact]
        public async void AsyncStaticMethodTest()
        {
            var fake = new Fake<AsyncTestClass>();

            fake.Replace(() => AsyncTestClass.GetStaticDynamicValueAsync()).Returns(Task.FromResult(7));

            Assert.Equal(7, await fake.Rewrite(() => AsyncTestClass.GetStaticValueAsync()).Execute());
        }

        [Fact]
        public void ParamsMethodTest()
        {
            var fake = new Fake<ParamsTestClass>();
            fake.Replace(p => p.GetValue(1, 2, 3)).Returns(-1);

            Assert.Equal(-1, fake.Rewrite(f => f.Test()).Execute());

            fake = new Fake<ParamsTestClass>();
            var values = new[] {1, 2, 3};
            fake.Replace(p => p.GetValue(values)).Returns(-1);

            Assert.Equal(-1, fake.Rewrite(f => f.Test()).Execute());
        }

        [Fact]
        public void LambdaArgumentTest()
        {
            var asserDate = new DateTime(2016, 11, 04);
            var fake = new Fake<TestClass>();
            var zones = TimeZoneInfo.GetSystemTimeZones();
            var correctZone = zones[0];
            var failedZone = zones[1];

            fake.Verify(() => TimeZoneInfo.ConvertTimeFromUtc(Arg.Is<DateTime>(d => d > asserDate),
                Arg.Is<TimeZoneInfo>(t => !Equals(t, failedZone)))).CheckArguments();

            fake.Rewrite(f => f.GetValueByArguments(Arg.DefaultOf<DateTime>(), Arg.DefaultOf<TimeZoneInfo>()));

            Assert.Throws<VerifiableException>(() => fake.Execute(f => f.GetValueByArguments(new DateTime(2016, 11, 05), failedZone)));
            Assert.Throws<VerifiableException>(() => fake.Execute(f => f.GetValueByArguments(new DateTime(2016, 11, 03), correctZone)));
            Assert.Throws<VerifiableException>(() => fake.Execute(f => f.GetValueByArguments(new DateTime(2016, 11, 03), failedZone)));
            fake.Execute(f => f.GetValueByArguments(new DateTime(2016, 11, 05), correctZone));
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

        private class ParamsTestClass
        {
            public int GetValue(params int[] values) => values.Sum();

            public int Test()
            {
                Debug.WriteLine("Started");
                var value = GetValue();
                Debug.WriteLine("Finished");
                return value;
            }
        }
    }
}
