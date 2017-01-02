using System;
using System.Diagnostics;
using AutoFake.Exceptions;
using Xunit;

namespace AutoFake.IntegrationTests
{
    public class VerifiableTests
    {
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

        [Fact]
        public void CheckArgumentsTest()
        {
            var fake = new Fake<TestClass>();

            var date = DateTime.UtcNow;
            var zones = TimeZoneInfo.GetSystemTimeZones();
            var setupZone = zones[0];
            var failedZone = zones[1];

            fake.Verify(() => TimeZoneInfo.ConvertTimeFromUtc(date, setupZone))
                .CheckArguments();

            fake.Rewrite(f => f.GetValueByArguments(Arg.DefaultOf<DateTime>(), Arg.DefaultOf<TimeZoneInfo>()));

            Assert.Throws<VerifiableException>(() => fake.Execute(f => f.GetValueByArguments(DateTime.MinValue, setupZone)));
            Assert.Throws<VerifiableException>(() => fake.Execute(f => f.GetValueByArguments(date, failedZone)));

            Assert.Equal(TimeZoneInfo.ConvertTimeFromUtc(date, setupZone), fake.Execute(f => f.GetValueByArguments(date, setupZone)));
        }

        [Fact]
        public void ExpectedCallsCountTest()
        {
            var fake = new Fake<TestClass>();

            var date = DateTime.UtcNow;
            var zone = TimeZoneInfo.Local;
            fake.Verify(() => TimeZoneInfo.ConvertTimeFromUtc(date, zone))
                .ExpectedCallsCount(2);

            Assert.Throws<ExpectedCallsException>(() => fake.Rewrite(f => f.GetValueByArguments(date, zone)).Execute());

            fake = new Fake<TestClass>();
            fake.Verify(() => TimeZoneInfo.ConvertTimeFromUtc(date, zone))
                .ExpectedCallsCount(1);

            Assert.Equal(TimeZoneInfo.ConvertTimeFromUtc(date, zone), fake.Rewrite(f => f.GetValueByArguments(date, zone)).Execute());
        }

        [Fact]
        public void BranchesTest()
        {
            var fake = new Fake<TestClass>();
            fake.Verify((TestClass t) => t.CodeBranch(1, 2))
                .ExpectedCallsCount(2);

            Assert.Equal(6, fake.Rewrite(f => f.Sum(1, 2)).Execute());

            fake = new Fake<TestClass>();
            fake.Verify((TestClass t) => t.CodeBranch(0, 0))
                .ExpectedCallsCount(1);

            Assert.Equal(0, fake.Rewrite(f => f.Sum(0, 1)).Execute());
        }
    }
}
