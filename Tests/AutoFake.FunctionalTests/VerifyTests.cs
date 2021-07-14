using System;
using System.Diagnostics;
using System.Threading.Tasks;
using AutoFake.Exceptions;
using FluentAssertions;
using MultipleReturnTest;
using Xunit;

namespace AutoFake.FunctionalTests
{
    public class VerifyTests
    {
        [Theory]
        [InlineData(false, false, true)]
        [InlineData(true, false, true)]
        [InlineData(false, true, true)]
        [InlineData(true, true, false)]
        public void CheckArgumentsTest(bool correctDate, bool correctZone, bool throws)
        {
            var fake = new Fake<TestClass>();
            var date = correctDate ? new DateTime(2019, 1, 1) : DateTime.MinValue;
            var zone = correctZone ? TimeZoneInfo.Utc
                : TimeZoneInfo.CreateCustomTimeZone("incorrect", TimeSpan.FromHours(-6), "", "");

            var sut = fake.Rewrite(f => f.GetValueByArguments(date, zone));
            sut.Verify(() => TimeZoneInfo.ConvertTimeFromUtc(new DateTime(2019, 1, 1), TimeZoneInfo.Utc));

            if (throws)
            {
                Assert.Throws<VerifyException>(() => sut.Execute());
            }
            else
            {
                sut.Execute();
            }
        }

        [Theory]
        [InlineData("==", 2, true)]
        [InlineData("==", 1, false)]
        [InlineData("<", 1, true)]
        [InlineData(">", 0, false)]
        public void ExpectedCallsCountTest(string op, int arg, bool throws)
        {
            var fake = new Fake<TestClass>();
            Func<uint, bool> checker;
            switch (op)
            {
                case "==": checker = x => x == arg; break;
                case ">": checker = x => x > arg; break;
                case "<": checker = x => x < arg; break;
                default: throw new InvalidOperationException();
            }
            var originalDate = new DateTime(2019, 1, 1);
            var zone = TimeZoneInfo.CreateCustomTimeZone("correct", TimeSpan.FromHours(6), "", "");

            var sut = fake.Rewrite(f => f.GetValueByArguments(originalDate, zone));
            sut.Verify(() => TimeZoneInfo.ConvertTimeFromUtc(originalDate, zone))
                .ExpectedCalls(checker);

            if (throws)
            {
                Assert.Throws<ExpectedCallsException>(() => sut.Execute());
            }
            else
            {
                Assert.Equal(new DateTime(2019, 1, 1, 6, 0, 0), sut.Execute());
            }
        }

        [Fact]
        public void BranchesTest()
        {
            var fake = new Fake<TestClass>();
            var sut = fake.Rewrite(f => f.Sum(1, 2));
            sut.Verify(t => t.CodeBranch(1, 2))
                .ExpectedCalls(2);

            Assert.Equal(6, sut.Execute());

            fake = new Fake<TestClass>();
            sut = fake.Rewrite(f => f.Sum(0, 1));
            sut.Verify(t => t.CodeBranch(0, 0))
                .ExpectedCalls(1);

            Assert.Equal(0, sut.Execute());
        }

        [Fact]
        public async Task AsyncTest()
        {
            var fake = new Fake<TestClass>();

            var sut = fake.Rewrite(f => f.DoSomethingAsync());
            sut.Verify(f => f.Sum(1, 2))
                .ExpectedCalls(i => i == 1);

            await sut.Execute();
        }

        [Theory]
        [InlineData(-10, -1)]
        [InlineData(0, 0)]
        [InlineData(10, 1)]
        public void MultipleReturnTest_Success(int arg, int expected)
        {
	        var fake = new Fake<SystemUnderTest>();

	        var sut = fake.Rewrite(f => f.ConditionalReturn(arg));
	        sut.Verify(s => s.PrintAndReturn(arg, expected));

	        sut.Execute().Should().Be(expected);
        }

        [Theory]
        [InlineData(-10, 1)]
        [InlineData(0, 123)]
        [InlineData(10, -1)]
        public void MultipleReturnTest_Throws(int arg, int expected)
        {
	        var fake = new Fake<SystemUnderTest>();

	        var sut = fake.Rewrite(f => f.ConditionalReturn(arg));
	        sut.Verify(s => s.PrintAndReturn(arg, expected));
	        Action act = () => sut.Execute();

	        act.Should().Throw<VerifyException>();
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
				return CodeBranch(0, 0);
			}

			public Task DoSomethingAsync() => Task.Run(() => Sum(1, 2));
        }
    }
}
