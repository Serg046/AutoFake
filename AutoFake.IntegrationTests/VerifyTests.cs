using System;
using System.Diagnostics;
using System.Threading.Tasks;
using AutoFake.Exceptions;
using Xunit;

namespace AutoFake.IntegrationTests
{
    public class VerifyTests
    {
        [Fact]
        public void CheckArgumentsTest()
        {
            var fake = new Fake<TestClass>();

            fake.Rewrite(f => f.GetValueByArguments(Arg.IsAny<DateTime>(), Arg.IsAny<TimeZoneInfo>()))
                .Verify(() => TimeZoneInfo.ConvertTimeFromUtc(new DateTime(2019, 1, 1), TimeZoneInfo.Utc)).CheckArguments();

            fake.Execute(tst =>
            {
                var incorrectZone = TimeZoneInfo.CreateCustomTimeZone("incorrect", TimeSpan.FromHours(-6), "", "");
                Assert.Throws<VerifyException>(() => tst.GetValueByArguments(DateTime.MinValue, TimeZoneInfo.Utc));
                Assert.Throws<VerifyException>(() => tst.GetValueByArguments(new DateTime(2019, 1, 1), incorrectZone));
                Assert.Equal(TimeZoneInfo.ConvertTimeFromUtc(new DateTime(2019, 1, 1), TimeZoneInfo.Utc),
                    tst.GetValueByArguments(new DateTime(2019, 1, 1), TimeZoneInfo.Utc));
            });
        }

        [Fact]
        public void ExpectedCallsCountTest()
        {
            var fake = new Fake<TestClass>();
            fake.Rewrite(f => f.GetValueByArguments(DateTime.UtcNow, TimeZoneInfo.Local))
                .Verify(() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.Local))
                .ExpectedCalls(i => i == 2);
            fake.Execute(tst => Assert.Throws<ExpectedCallsException>(() => tst.GetValueByArguments(new DateTime(2019, 1, 1),
                TimeZoneInfo.CreateCustomTimeZone("correct", TimeSpan.FromHours(6), "", ""))));

            fake = new Fake<TestClass>();
            fake.Rewrite(f => f.GetValueByArguments(DateTime.UtcNow, TimeZoneInfo.Local))
                .Verify(() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.Local))
                .ExpectedCalls(i => i == 1);
            fake.Execute(tst => Assert.Equal(new DateTime(2019, 1, 1, 6, 0, 0),
                tst.GetValueByArguments(new DateTime(2019, 1, 1),
                    TimeZoneInfo.CreateCustomTimeZone("correct", TimeSpan.FromHours(6), "", ""))));

            fake = new Fake<TestClass>();
            fake.Rewrite(f => f.GetValueByArguments(DateTime.UtcNow, TimeZoneInfo.Local))
                .Verify(() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.Local))
                .ExpectedCalls(x => x < 1);
            fake.Execute(tst => Assert.Throws<ExpectedCallsException>(() => tst.GetValueByArguments(new DateTime(2019, 1, 1),
                TimeZoneInfo.CreateCustomTimeZone("correct", TimeSpan.FromHours(6), "", ""))));

            fake = new Fake<TestClass>();
            fake.Rewrite(f => f.GetValueByArguments(DateTime.UtcNow, TimeZoneInfo.Local))
                .Verify(() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.Local))
                .ExpectedCalls(x => x > 0);
            fake.Execute(tst => Assert.Equal(new DateTime(2019, 1, 1, 6, 0, 0),
                tst.GetValueByArguments(new DateTime(2019, 1, 1),
                    TimeZoneInfo.CreateCustomTimeZone("correct", TimeSpan.FromHours(6), "", ""))));
        }

        [Fact]
        public void BranchesTest()
        {
            var fake = new Fake<TestClass>();
            fake.Rewrite(f => f.Sum(1, 2))
                .Verify(t => t.CodeBranch(1, 2))
                .CheckArguments()
                .ExpectedCalls(i => i == 1);

            fake.Execute(tst => Assert.Equal(6, tst.Sum(1, 2)));

            fake = new Fake<TestClass>();
            fake.Rewrite(f => f.Sum(0, 1))
                .Verify(t => t.CodeBranch(0, 0))
                .ExpectedCalls(i => i == 1);

            fake.Execute(tst => Assert.Equal(0, tst.Sum(0, 1)));
        }

        [Fact]
        public async Task AsyncTest()
        {
            var fake = new Fake<TestClass>();

            fake.Rewrite(f => f.DoSomethingAsync())
                .Verify(f => f.Sum(1, 2))
                .CheckArguments()
                .ExpectedCalls(i => i == 1);

            await fake.ExecuteAsync(tst => tst.DoSomethingAsync());
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
            //    return CodeBranch(0, 0);
            //}

            public int Sum(int a, int b)
            {
                if (a > 0)
                {
                    return CodeBranch(a, b) + 3;
                }
                return CodeBranch(0, 0);
            }

            public Task DoSomethingAsync() => Task.Run(() => Sum(1, 2));
        }
    }
}
