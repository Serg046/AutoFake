using System;
using System.Diagnostics;
using AutoFake.Exceptions;
using Xunit;

namespace AutoFake.IntegrationTests
{
    public class ReplaceTests
    {
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

        [Fact]
        public void Replace_SourceAsmInstance_Replaced()
        {
            var date = new DateTime(2020, 5, 23);
            var fake = new Fake<TestClass>();

            fake.Rewrite(f => f.GetValueByArguments(Arg.IsAny<DateTime>(), Arg.IsAny<TimeZoneInfo>()))
                .Replace(() => TimeZoneInfo.ConvertTimeFromUtc(new DateTime(2019, 1, 1), TimeZoneInfo.Utc))
                .CheckArguments()
                .Return(date);

            fake.Execute(tst =>
            {
                Assert.Equal(date, tst.GetValueByArguments(new DateTime(2019, 1, 1), TimeZoneInfo.Utc));
            });
        }

        [Fact]
        public void Replace_Class_Replaced()
        {
            const int mutator = 4;
            var cl2 = new TestClass2()
            {
                Value = 2
            };
            var fake = new Fake<TestClass>();

            fake.Rewrite(f => f.MutateTestClass2(Arg.IsAny<TestClass2>(), mutator))
                .Replace(f => f.Mutator(Arg.IsAny<TestClass2>(), mutator))
                .Return(cl2);

            fake.Execute(tst =>
            {
                var t1 = new TestClass2();
                Assert.Equal(cl2, tst.MutateTestClass2(t1, mutator));
            });
        }

        public class TestClass
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

            public TestClass2 MutateTestClass2(TestClass2 cl, int value)
            {
                return Mutator(cl, value);
            }

            public TestClass2 Mutator(TestClass2 cl, int value)
            {
                cl.Value = value;
                return cl;
            }
        }

        public class TestClass2
        {
            public int Value { get; set; }
        }
    }
}
