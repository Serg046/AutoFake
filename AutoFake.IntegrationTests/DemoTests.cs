using System;
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
            internal Task<DateTime> AddSomeMinutesAsync(DateTime date) => Task.Run(() => AddSomeMinutes(date));
            public static DateTime AddSomeMinutes(DateTime date) => date.AddMinutes(new Random().Next(1, 10));
        }

        [Fact]
        public void Yesterday_SomeDay_ThePrevDay()
        {
            var fake = new Fake<Calendar>();

            var sut = fake.Rewrite(() => Calendar.Yesterday);
            sut.Replace(() => DateTime.Now).Return(new DateTime(2016, 8, day: 8));

            Assert.Equal(new DateTime(2016, 8, 7), sut.Execute());
        }

        [Fact]
        public async Task AddSomeMinutesAsync_SomeDay_MinutesAdded()
        {
            var randomValue = 7;
            var date = new DateTime(2016, 8, 8, hour: 0, minute: 0, second: 0);
            var fake = new Fake<Calendar>();

            var sut = fake.Rewrite(f => f.AddSomeMinutesAsync(date));
            sut.Replace((Random r) => r.Next(1, 10)) // Arg.Is<int>(i => i == 10) is also possible
                .CheckArguments() // r.Next(1, 11) fails with "Expected - 11, actual - 10"
                .ExpectedCalls(c => c > 0) // c => c > 1 fails with "Actual value - 1"
                .Return(randomValue);

            Assert.Equal(date.AddMinutes(randomValue), await sut.Execute());
        }

        [Fact]
        public void AddSomeMinutes_SomeDay_EventsRecorded()
        {
            var events = new List<string>();
            var fake = new Fake<Calendar>();

            var sut = fake.Rewrite(() => Calendar.AddSomeMinutes(new DateTime(2016, 8, 8)));

            sut.Prepend(() => events.Add("The first line"));
            sut.Prepend(() => events.Add("The line before AddMinutes(...) call"))
                .Before((DateTime date) => date.AddMinutes(Arg.IsAny<int>()));

            sut.Append(() => events.Add("The line after new Random() call"))
                .After(() => new Random());
            sut.Append(() => events.Add("The last line"));

            sut.Execute();
            Assert.Equal(new[]
                {
                    "The first line",
                    "The line after new Random() call", // indeed, this call is earlier
                    "The line before AddMinutes(...) call",
                    "The last line"
                },
                events);
        }
    }
}
