using System;
using System.Diagnostics;
using AutoFake.Exceptions;
using Xunit;

namespace AutoFake.IntegrationTests
{
    public class ReplacementTests
    {
        [Fact]
        public void CheckArgumentsTest()
        {
            //var fake = new Fake<TestClass>();

            //var date = DateTime.UtcNow;
            //var zones = TimeZoneInfo.GetSystemTimeZones();
            //var setupZone = zones[0];
            //var failedZone = zones[1];

            //fake.Replace(() => TimeZoneInfo.ConvertTimeFromUtc(date, setupZone))
            //    .CheckArguments()
            //    .Returns(DateTime.MinValue);

            //fake.Rewrite(f => f.GetValueByArguments(Arg.DefaultOf<DateTime>(), Arg.DefaultOf<TimeZoneInfo>()));

            //Assert.Throws<VerifiableException>(() => fake.Execute(f => f.GetValueByArguments(DateTime.MinValue, setupZone)));
            //Assert.Throws<VerifiableException>(() => fake.Execute(f => f.GetValueByArguments(date, failedZone)));

            //Assert.Equal(DateTime.MinValue, fake.Execute(f => f.GetValueByArguments(date, setupZone)));
            throw new NotImplementedException();
        }

        [Fact]
        public void ExpectedCallsCountTest()
        {
            var fake = new Fake<TestClass>();
            fake.Replace(() => TimeZoneInfo.ConvertTimeFromUtc(default(DateTime), default(TimeZoneInfo)))
                .ExpectedCallsCount(2)
                .Returns(() => DateTime.MinValue);
            fake.Rewrite(f => f.GetValueByArguments(DateTime.MinValue, null));

            fake.Execute2(tst => Assert.Throws<ExpectedCallsException>(() => tst.GetValueByArguments(DateTime.MinValue, null)));

            fake = new Fake<TestClass>();
            fake.Replace(() => TimeZoneInfo.ConvertTimeFromUtc(default(DateTime), default(TimeZoneInfo)))
                .ExpectedCallsCount(1)
                .Returns(() => DateTime.MinValue);
            fake.Rewrite(f => f.GetValueByArguments(DateTime.MinValue, null));

            fake.Execute2(tst => Assert.Equal(DateTime.MinValue, tst.GetValueByArguments(DateTime.MinValue, null)));

            fake = new Fake<TestClass>();
            fake.Replace(() => TimeZoneInfo.ConvertTimeFromUtc(default(DateTime), default(TimeZoneInfo)))
                .ExpectedCallsCount(x => x > 0)
                .Returns(() => DateTime.MinValue);
            fake.Rewrite(f => f.GetValueByArguments(DateTime.MinValue, null));

            fake.Execute2(tst => Assert.Equal(DateTime.MinValue, tst.GetValueByArguments(DateTime.MinValue, null)));
        }

        [Fact]
        public void BranchesTest()
        {
            var fake = new Fake<TestClass>();
            fake.Replace(t => t.CodeBranch(1, 2))
                .CheckArguments()
                .ExpectedCallsCount(2)
                .Returns(() => 6);
            fake.Rewrite(f => f.Sum(1, 2));

            fake.Execute2(tst => Assert.Equal(12, tst.Sum(1, 2)));

            fake = new Fake<TestClass>();
            fake.Replace(t => t.CodeBranch(0, 0))
                .CheckArguments()
                .ExpectedCallsCount(1)
                .Returns(() => 6);
            fake.Rewrite(f => f.Sum(0, 1));

            fake.Execute2(tst => Assert.Equal(6, tst.Sum(0, 1)));
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

            public int Sum(int a, int b)
            {
                if (a > 0)
                {
                    return CodeBranch(a, b) + CodeBranch(a, b);
                }
                else
                {
                    return CodeBranch(0, 0);
                }
            }
        }
    }
}
