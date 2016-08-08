using AutoFake;
using System;
using System.Linq;
using Xunit;

namespace UnitTests
{
    public class Calendar
    {
        private readonly TimeZoneInfo _timeZone;

        public Calendar(TimeZoneInfo timeZone)
        {
            _timeZone = timeZone;
        }

        public DateTime GetDate()
        {
            return TimeZoneInfo.ConvertTime(DateTime.Now, _timeZone);
        }

        public DateTime GetDateWithInnerCall()
        {
            return TimeZoneInfo.ConvertTime(GetCurrentDate(), _timeZone);
        }

        private DateTime GetCurrentDate() => DateTime.Now;
    }

    public class FakeTests
    {
        [Fact]
        public void SimpleTest()
        {
            var currentDate = new DateTime(2016, 8, 8, 15, 00, 00, DateTimeKind.Local); 

            var timeZone = TimeZoneInfo.GetSystemTimeZones().Single(t => t.Id == "UTC");
            var calendarFake = Fake.For<Calendar>(timeZone)
                .Setup(c => DateTime.Now)
                .ReachableWith(c => c.GetDate())
                .Returns(currentDate);

            Assert.Equal(new DateTime(2016, 8, 8, 12, 00, 00), calendarFake.Execute(c => c.GetDate()));
        }

        [Fact]
        public void InnerCallOfMockedMethodWorksFine()
        {
            var currentDate = new DateTime(2016, 8, 8, 15, 00, 00, DateTimeKind.Local);

            var timeZone = TimeZoneInfo.GetSystemTimeZones().Single(t => t.Id == "UTC");
            var calendarFake = Fake.For<Calendar>(timeZone)
                .Setup(c => DateTime.Now)
                .ReachableWith(c => c.GetDateWithInnerCall())
                .Returns(currentDate);

            Assert.Equal(new DateTime(2016, 8, 8, 12, 00, 00), calendarFake.Execute(c => c.GetDateWithInnerCall()));
        }
    }
}
