using System;
using System.Diagnostics;
using AutoFake.Exceptions;
using Xunit;

namespace AutoFake.IntegrationTests
{
    public class ReplacementTests
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
            var zone = TimeZoneInfo.Local;
            fake.Replace(() => TimeZoneInfo.ConvertTimeFromUtc(date, zone))
                .CheckArguments()
                .Returns(DateTime.MinValue);

            fake.Rewrite(f => f.GetValueByArguments(Arg.DefaultOf<DateTime>(), Arg.DefaultOf<TimeZoneInfo>()));

            Assert.Throws<VerifiableException>(() => fake.Execute(f => f.GetValueByArguments(DateTime.MinValue, zone)));
            Assert.Throws<VerifiableException>(() => fake.Execute(f => f.GetValueByArguments(date, TimeZoneInfo.Utc)));

            Assert.Equal(DateTime.MinValue, fake.Execute(f => f.GetValueByArguments(date, zone)));
        }

        [Fact]
        public void ExpectedCallsCountTest()
        {
            var fake = new Fake<TestClass>();
            fake.Replace(() => TimeZoneInfo.ConvertTimeFromUtc(default(DateTime), default(TimeZoneInfo)))
                .ExpectedCallsCount(2)
                .Returns(DateTime.MinValue);

            Assert.Throws<ExpectedCallsException>(() => fake.Rewrite(f => f.GetValueByArguments(DateTime.MinValue, null)).Execute());

            fake = new Fake<TestClass>();
            fake.Replace(() => TimeZoneInfo.ConvertTimeFromUtc(default(DateTime), default(TimeZoneInfo)))
                .ExpectedCallsCount(1)
                .Returns(DateTime.MinValue);

            Assert.Equal(DateTime.MinValue, fake.Rewrite(f => f.GetValueByArguments(DateTime.MinValue, null)).Execute());
        }

        [Fact]
        public void BranchesTest()
        {
            var fake = new Fake<TestClass>();
            fake.Replace((TestClass t) => t.CodeBranch(1, 2))
                .CheckArguments()
                .ExpectedCallsCount(2)
                .Returns(6);

            Assert.Equal(12, fake.Rewrite(f => f.Sum(1, 2)).Execute());

            fake = new Fake<TestClass>();
            fake.Replace((TestClass t) => t.CodeBranch(0, 0))
                .CheckArguments()
                .ExpectedCallsCount(1)
                .Returns(6);

            Assert.Equal(6, fake.Rewrite(f => f.Sum(0, 1)).Execute());
        }
    }
}
