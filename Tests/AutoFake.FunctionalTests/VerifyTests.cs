﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AutoFake.Abstractions;
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
        public void When_arguments_are_passed_Should_be_checked(bool correctDate, bool correctZone, bool throws)
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
        public void When_expected_calls_configured_Should_check(string op, int arg, bool throws)
        {
            var fake = new Fake<TestClass>();
            IExecutionContext.CallsCheckerFunc checker;
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
        public void When_there_are_branches_Should_pass()
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
            sut.Verify(f => f.Sum(1, 2)).ExpectedCalls(i => i == 1);

            await sut.Execute();
        }

        [Fact]
        public void When_enumerable_Should_verify()
        {
            var fake = new Fake<TestClass>();

            var sut = fake.Rewrite(f => f.DoSomethingInIterator());
            sut.Verify(f => f.Sum(1, 2)).ExpectedCalls(i => i == 1);

            sut.Execute().Cast<int>().Sum().Should().Be(6);
        }

        [Fact]
        public void When_typed_enumerable_Should_verify()
        {
            var fake = new Fake<TestClass>();

            var sut = fake.Rewrite(f => f.DoSomethingInTypedIterator());
            sut.Verify(f => f.Sum(1, 2)).ExpectedCalls(i => i == 1);

            sut.Execute().Sum().Should().Be(6);
        }

#if NETCOREAPP3_0
        [Fact]
        public async Task When_async_enumerable_Should_verify()
        {
            var fake = new Fake<TestClass>();

            var sut = fake.Rewrite(f => f.DoSomethingInAsyncIterator());
            sut.Verify(f => f.Sum(1, 2)).ExpectedCalls(i => i == 1);

            await foreach (var value in sut.Execute())
            {
                value.Should().Be(6);
            }
        }
#endif

        [Theory]
        [InlineData(-10, -1)]
        [InlineData(0, 0)]
        [InlineData(10, 1)]
        public void When_multiple_return_with_matched_args_Should_succeed(int arg, int expected)
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
        public void When_multiple_return_without_matched_args_Should_fail(int arg, int expected)
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

			public async Task DoSomethingAsync() => await Task.Run(() => Sum(1, 2));

			public IEnumerable DoSomethingInIterator()
			{
				yield return Sum(1, 2);
			}

            public IEnumerable<int> DoSomethingInTypedIterator()
            {
                yield return Sum(1, 2);
            }

#if NETCOREAPP3_0
            public async IAsyncEnumerable<int> DoSomethingInAsyncIterator()
            {
                await Task.Yield();
                yield return Sum(1, 2);
            }
#endif
        }
    }
}
