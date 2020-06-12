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

            var sut = fake.Rewrite(f => f.GetDynamicValue());
            sut.Replace(t => t.DynamicValue()).Return(() => 7);

            Assert.Equal(7, sut.Execute());
        }

        [Fact]
        public void ExternalInstanceTest()
        {
            var fake = new Fake<TestClass>();

            fake.Rewrite(f => f.GetHelperDynamicValue())
                .Replace((HelperClass h) => h.DynamicValue()).Return(() => 7);
            
            fake.Execute(tst => Assert.Equal(7, tst.GetHelperDynamicValue()));
        }

        [Fact]
        public void OwnStaticTest()
        {
            var fake = new Fake<TestClass>();

            fake.Rewrite(f => f.GetDynamicStaticValue())
                .Replace(() => TestClass.DynamicStaticValue()).Return(() => 7);

            fake.Execute(tst => Assert.Equal(7, tst.GetDynamicStaticValue()));
        }

        [Fact]
        public void ExternalStaticTest()
        {
            var fake = new Fake<TestClass>();

            fake.Rewrite(f => f.GetHelperDynamicStaticValue())
                .Replace(() => HelperClass.DynamicStaticValue()).Return(() => 7);

            fake.Execute(tst => Assert.Equal(7, tst.GetHelperDynamicStaticValue()));
        }

        [Fact]
        public void FrameworkInstanceTest()
        {
            var fake = new Fake<TestClass>();

            fake.Rewrite(f => f.GetFrameworkValue())
                .Replace((SqlCommand c) => c.ExecuteScalar()).Return(() => 7);

            fake.Execute(tst => Assert.Equal(7, tst.GetFrameworkValue()));
        }

        [Fact]
        public void FrameworkStaticTest()
        {
            var fake = new Fake<TestClass>();

            fake.Rewrite(f => f.GetFrameworkStaticValue())
                .Replace(() => CollectionsUtil.CreateCaseInsensitiveHashtable()).Return(() => new Hashtable {{ 1, 1 }});

            fake.Execute((tst, prm) => Assert.Equal(prm[0], tst.GetFrameworkStaticValue()));
        }

        [Fact]
        public void MultipleArgumentsTest()
        {
            var fake = new Fake<TestClass>();

            fake.Rewrite(t => t.GetValueByArguments(DateTime.UtcNow, TimeZoneInfo.Local))
                .Replace(() => TimeZoneInfo.ConvertTimeFromUtc(
                    Arg.IsAny<DateTime>(), Arg.IsAny<TimeZoneInfo>())).Return(() => DateTime.MinValue);

            fake.Execute(tst => Assert.Equal(DateTime.MinValue, tst.GetValueByArguments(DateTime.UtcNow, TimeZoneInfo.Local)));
        }

        [Fact]
        public void VoidMethodMockTest()
        {
            Assert.Throws<NotImplementedException>(() => new TestClass().UnsafeMethod());

            var fake = new Fake<TestClass>();

            fake.Rewrite(f => f.UnsafeMethod())
                .Remove(t => t.ThrowException());

            //no exception
            fake.Execute(tst => tst.UnsafeMethod());
        }

        [Fact]
        public void OverloadedMethodTest()
        {
            var fake = new Fake<TestClass>();

            var method = fake.Rewrite(f => f.GetDynValueByOveloadedMethodCalls());
            method.Replace(t => t.DynamicValue()).Return(() => 7);
            method.Replace(t => t.DynamicValue(5)).Return(() => 7);

            fake.Execute(tst => Assert.Equal(14, tst.GetDynValueByOveloadedMethodCalls()));
        }

        [Fact]
        public async Task AsyncInstanceMethodTest()
        {
            var fake = new Fake<AsyncTestClass>();

            fake.Rewrite(f => f.GetValueAsync())
                .Replace(a => a.GetDynamicValueAsync()).Return(() => Task.FromResult(7));

            await fake.ExecuteAsync(async tst => Assert.Equal(7, await tst.GetValueAsync()));
        }

        [Fact]
        public async Task AsyncStaticMethodTest()
        {
            var fake = new Fake<AsyncTestClass>();

            fake.Rewrite(f => f.GetStaticValueAsync())
                .Replace(() => AsyncTestClass.GetStaticDynamicValueAsync()).Return(() => Task.FromResult(7));

            await fake.ExecuteAsync(async tst => Assert.Equal(7, await tst.GetStaticValueAsync()));
        }

        [Fact]
        public void ParamsMethodTest()
        {
            var fake = new Fake<ParamsTestClass>();
            fake.Rewrite(f => f.Test())
                .Replace(p => p.GetValue(1, 2, 3)).Return(() => -1);

            fake.Execute(tst => Assert.Equal(-1, tst.Test()));

            fake = new Fake<ParamsTestClass>();
            fake.Rewrite(f => f.Test())
                .Replace(p => p.GetValue(new[] { 1, 2, 3 })).Return(() => -1);

            fake.Execute(tst => Assert.Equal(-1, tst.Test()));
        }

        [Fact]
        public void LambdaArgumentTest()
        {
            var fake = new Fake<TestClass>();

            fake.Rewrite(f => f.GetValueByArguments(Arg.IsAny<DateTime>(), Arg.IsAny<TimeZoneInfo>()))
                .Verify(() => TimeZoneInfo.ConvertTimeFromUtc(Arg.Is<DateTime>(d => d > new DateTime(2016, 11, 04)),
                    Arg.Is<TimeZoneInfo>(t => t.BaseUtcOffset.Hours > 0))).CheckArguments();

            fake.Execute(tst =>
            {
                var correctZone = TimeZoneInfo.CreateCustomTimeZone("correct", TimeSpan.FromHours(6), "", "");
                var incorrectZone = TimeZoneInfo.CreateCustomTimeZone("incorrect", TimeSpan.FromHours(-6), "", "");
                Assert.Throws<VerifyException>(() => tst.GetValueByArguments(new DateTime(2016, 11, 05), incorrectZone));
                Assert.Throws<VerifyException>(() => tst.GetValueByArguments(new DateTime(2016, 11, 03), correctZone));
                Assert.Throws<VerifyException>(() => tst.GetValueByArguments(new DateTime(2016, 11, 03), incorrectZone));
                tst.GetValueByArguments(new DateTime(2016, 11, 05), correctZone);
            });
        }

        [Fact]
        public void RecursionTest()
        {
            var fake = new Fake<TestClass>();

            fake.Rewrite(f => f.GetRecursionValue(2))
                .Replace(f => f.GetRecursionValue(Arg.IsAny<int>())).Return(() => -1);

            fake.Execute(tst => Assert.Equal(-1, tst.GetRecursionValue(2)));
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
                var value = GetValue();
                Debug.WriteLine("Finished");
                return value;
            }
        }
    }
}
