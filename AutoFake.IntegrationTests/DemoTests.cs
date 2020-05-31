﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace AutoFake.IntegrationTests
{
    public class DemoTests
    {
        public class Calendar
        {
            public static DateTime Yesterday => DateTime.Now.AddDays(-1);
            internal Task<DateTime> AddSomeMinutesAsync(DateTime date)
                => Task.Run(() => date.AddMinutes(new Random().Next(1, 10)));
        }

        [Fact]
        public void Yesterday_SomeDay_ThePrevDay()
        {
            var fake = new Fake<Calendar>();

            fake.Rewrite(f => Calendar.Yesterday)
                .Replace(() => DateTime.Now)
                .Return(new DateTime(2016, 8, day: 8));

            fake.Execute(() => Assert.Equal(new DateTime(2016, 8, 7), Calendar.Yesterday));
        }

        [Fact]
        public async Task AddSomeMinutes_SomeDay_MinutesAdded()
        {
            var randomValue = 7;
            var events = new List<string>();
            var fake = new Fake<Calendar>();

            var addSomeMinutesAsync = fake.Rewrite(f => f.AddSomeMinutesAsync(Arg.IsAny<DateTime>()));
            addSomeMinutesAsync
                .Replace((Random r) => r.Next(1, 10)) // Arg.Is<int>(i => i == 10) is also possible
                .CheckArguments() // r.Next(1, 11) fails with "Expected - 11, actual - 10"
                .ExpectedCalls(c => c > 0) // c => c > 1 fails with "Actual value - 1"
                .Return(randomValue);

            addSomeMinutesAsync
                .Prepend(() => events.Add("The first line"));
            addSomeMinutesAsync
                .Prepend(() => events.Add("The line before AddMinutes(...) call"))
                .Before((DateTime date) => date.AddMinutes(Arg.IsAny<int>()));

            addSomeMinutesAsync
                .Append(() => events.Add("The line after new Random() call"))
                .After(() => new Random());
            addSomeMinutesAsync
                .Append(() => events.Add("The last line"));

            await fake.ExecuteAsync(async calendar =>
            {
                Assert.Equal(new DateTime(2016, 8, 8, 0, minute: randomValue, 0),
                    await calendar.AddSomeMinutesAsync(new DateTime(2016, 8, 8, hour: 0, minute: 0, second: 0)));
                Assert.Equal(new[]
                    {
                        "The first line",
                        "The line after new Random() call", // indeed, this call is earlier
                        "The line before AddMinutes(...) call",
                        "The last line"
                    },
                    events);
            });
        }
    }
}