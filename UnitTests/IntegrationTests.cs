using AutoFake;
using System;
using System.Linq;
using Xunit;

namespace UnitTests
{
    public class IntegrationTests
    {
        [Fact]
        public void StaticPropertyMockWorksFine()
        {
            var currentDate = new DateTime(2016, 8, 8, 15, 00, 00, DateTimeKind.Local); 

            var timeZone = TimeZoneInfo.GetSystemTimeZones().Single(t => t.Id == "UTC");
            var calendarFake = Fake.For<Calendar>(timeZone)
                .Setup(() => DateTime.Now)
                .ReachableWith(c => c.GetDate())
                .Returns(currentDate);

            var expectedDate = new DateTime(2016, 8, 8, 12, 00, 00);
            Assert.Equal(expectedDate, calendarFake.Execute(c => c.GetDate()));
        }

        [Fact]
        public void InnerCallOfMockedMethodWorksFine()
        {
            var currentDate = new DateTime(2016, 8, 8, 15, 00, 00, DateTimeKind.Local);

            var timeZone = TimeZoneInfo.GetSystemTimeZones().Single(t => t.Id == "UTC");
            var calendarFake = Fake.For<Calendar>(timeZone)
                .Setup(() => DateTime.Now)
                .ReachableWith(c => c.GetDateWithInnerCall())
                .Returns(currentDate);

            var expectedDate = new DateTime(2016, 8, 8, 12, 00, 00);
            Assert.Equal(expectedDate, calendarFake.Execute(c => c.GetDateWithInnerCall()));
        }

        [Fact]
        public void MethodMockWorksFine()
        {
            var currentDate = new DateTime(2016, 8, 8, 15, 00, 00, DateTimeKind.Local);

            var timeZone = TimeZoneInfo.GetSystemTimeZones().Single(t => t.Id == "UTC");
            var calendarFake = Fake.For<Calendar>(timeZone)
                .Setup((Calendar c) => c.GetCurrentDate())
                .ReachableWith(c => c.GetDateWithInnerCall())
                .Returns(currentDate);

            var expectedDate = new DateTime(2016, 8, 8, 12, 00, 00);
            Assert.Equal(expectedDate, calendarFake.Execute(c => c.GetDateWithInnerCall()));
        }

        [Fact]
        public void ExternalMethodMockWorksFine()
        {
            var currentDate = new DateTime(2016, 8, 8, 15, 00, 00, DateTimeKind.Local);
            var offset = 1;

            var timeZone = TimeZoneInfo.GetSystemTimeZones().Single(t => t.Id == "UTC");
            var calendarFake = Fake.For<Calendar>(timeZone)
                .Setup((DateCalculator dc) => dc.AddHours(currentDate, offset))
                .ReachableWith(c => c.GetDateWithOffset(offset))
                .Returns(currentDate);
            
            Assert.Equal(currentDate, calendarFake.Execute(c => c.GetDateWithOffset(offset)));
        }

        [Fact]
        public void MultipleReachableWithWorksFine()
        {
            var currentDate = new DateTime(2016, 8, 8, 15, 00, 00, DateTimeKind.Local);

            var timeZone = TimeZoneInfo.GetSystemTimeZones().Single(t => t.Id == "UTC");
            var calendarFake = Fake.For<Calendar>(timeZone)
                .Setup(() => DateTime.Now)
                .ReachableWith(c => c.GetDate())
                .ReachableWith(c => c.GetDateWithInnerCall())
                .Returns(currentDate);

            var expectedDate = new DateTime(2016, 8, 8, 12, 00, 00);
            Assert.Equal(expectedDate, calendarFake.Execute(c => c.GetDate()));
            Assert.Equal(expectedDate, calendarFake.Execute(c => c.GetDateWithInnerCall()));
        }
    }
}
