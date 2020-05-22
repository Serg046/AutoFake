using System;
using System.Diagnostics;
using AutoFake.Exceptions;
using Xunit;

namespace AutoFake.IntegrationTests
{
    public class ReplaceTests
    {
        public class Calendar
        {
            public static DateTime Yesterday => DateTime.Now.AddDays(-1);
            internal DateTime AddSomeMinutes(DateTime date) => date.AddMinutes(new Random().Next(1, 10));
        }

        [Fact]
        public void Yesterday_SomeDay_ThePrevDay()
        {
            var fake = new Fake<Calendar>();

            fake.Rewrite(f => Calendar.Yesterday)
                .Replace(() => DateTime.Now)
                .Return(() => new DateTime(2016, 8, day: 8));

            fake.Execute(calendar => Assert.Equal(new DateTime(2016, 8, 7), Calendar.Yesterday));
        }

        [Fact]
        public void AddSomeMinutes_SomeDay_MinutesAdded()
        {
            var fake = new Fake<Calendar>();

            fake.Rewrite(f => f.AddSomeMinutes(Arg.IsAny<DateTime>()))
                .Replace((Random r) => r.Next(1, 10)) // Arg.Is<int>(i => i == 10) is also possible
                .CheckArguments() // r.Next(1, 11) fails with "Expected - 11, actual - 10"
                .ExpectedCalls(c => c > 0) // c => c > 1 fails with "Actual value - 1"
                .Return(() => 7);

            fake.Execute(calendar => Assert.Equal(new DateTime(2016, 8, 8, 0, minute: 7, 0),
                calendar.AddSomeMinutes(new DateTime(2016, 8, 8, hour: 0, minute: 0, second: 0))));
        }

        [Fact]
        public void CheckArgumentsTest()
        {
            var fake = new Fake<TestClass>();

            fake.Rewrite(f => f.GetValueByArguments(Arg.IsAny<DateTime>(), Arg.IsAny<TimeZoneInfo>()))
                .Replace(() => TimeZoneInfo.ConvertTimeFromUtc(new DateTime(2019, 1, 1), TimeZoneInfo.Utc))
                .CheckArguments()
                .Return(() => DateTime.MinValue);

            fake.Execute(tst =>
            {
                var incorrectZone = TimeZoneInfo.CreateCustomTimeZone("incorrect", TimeSpan.FromHours(-6), "", "");
                Assert.Throws<VerifyException>(() => tst.GetValueByArguments(DateTime.MinValue, TimeZoneInfo.Utc));
                Assert.Throws<VerifyException>(() => tst.GetValueByArguments(new DateTime(2019, 1, 1), incorrectZone));
                Assert.Equal(DateTime.MinValue, tst.GetValueByArguments(new DateTime(2019, 1, 1), TimeZoneInfo.Utc));
            });
        }

        [Fact]
        public void ExpectedCallsCountTest()
        {
            var fake = new Fake<TestClass>();
            fake.Rewrite(f => f.GetValueByArguments(DateTime.MinValue, null))
                .Replace(() => TimeZoneInfo.ConvertTimeFromUtc(default(DateTime), default(TimeZoneInfo)))
                .ExpectedCalls(i => i == 2)
                .Return(() => DateTime.MinValue);

            fake.Execute(tst => Assert.Throws<ExpectedCallsException>(() => tst.GetValueByArguments(DateTime.MinValue, null)));

            fake = new Fake<TestClass>();
            fake.Rewrite(f => f.GetValueByArguments(DateTime.MinValue, null))
                .Replace(() => TimeZoneInfo.ConvertTimeFromUtc(default(DateTime), default(TimeZoneInfo)))
                .ExpectedCalls(i => i == 1)
                .Return(() => DateTime.MinValue);

            fake.Execute(tst => Assert.Equal(DateTime.MinValue, tst.GetValueByArguments(DateTime.MinValue, null)));

            fake = new Fake<TestClass>();
            fake.Rewrite(f => f.GetValueByArguments(DateTime.MinValue, null))
                .Replace(() => TimeZoneInfo.ConvertTimeFromUtc(default(DateTime), default(TimeZoneInfo)))
                .ExpectedCalls(x => x > 0)
                .Return(() => DateTime.MinValue);

            fake.Execute(tst => Assert.Equal(DateTime.MinValue, tst.GetValueByArguments(DateTime.MinValue, null)));
        }

        [Fact]
        public void BranchesTest()
        {
            var fake = new Fake<TestClass>();
            fake.Rewrite(f => f.Sum(1, 2))
                .Replace(t => t.CodeBranch(1, 2))
                .CheckArguments()
                .ExpectedCalls(i => i == 1)
                .Return(() => 6);

            fake.Execute(tst => Assert.Equal(9, tst.Sum(1, 2)));

            fake = new Fake<TestClass>();
            fake.Rewrite(f => f.Sum(0, 1))
                .Replace(t => t.CodeBranch(0, 0))
                .CheckArguments()
                .ExpectedCalls(i => i == 1)
                .Return(() => 6);

            fake.Execute(tst => Assert.Equal(6, tst.Sum(0, 1)));
        }

        private class TestClass
        {
            public DateTime GetValueByArguments(DateTime dateTime, TimeZoneInfo zone)
            {
                Debug.WriteLine("Started");
                var value = TimeZoneInfo.ConvertTimeFromUtc(dateTime, zone);
                Debug.WriteLine("Finished");
                return value;
            }

            public int CodeBranch(int a, int b) => a + b;

            //public int Sum(int a, int b)
            //{
            //    if (a > 0)
            //    {
            //        return CodeBranch(a, b) + CodeBranch(a, b);
            //    }
            //    else
            //    {
            //        return CodeBranch(0, 0);
            //    }
            //}

            public int Sum(int a, int b)
            {
                if (a > 0)
                {
                    return CodeBranch(a, b) + 3;
                }
                return CodeBranch(0, 0);
            }
        }
    }
}
