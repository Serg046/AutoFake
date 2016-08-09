using AutoFake;
using System;
using Xunit;

namespace UnitTests
{
    public class IntegrationTests
    {
        private readonly DateTime _currentDate = new DateTime(2016, 8, 8, 15, 00, 00, DateTimeKind.Local);

        [Fact]
        public void InstancePropertyMockWorksFine()
        {
            var calendarFake = Fake.For<Calendar>(Calendar.GetTimeZone())
                .Setup((Calendar c) => c.UtcDate)
                .ReachableWith(c => c.GetUtcDate())
                .Returns(_currentDate);

            Assert.Equal(_currentDate, calendarFake.Execute(c => c.GetUtcDate()));
        }

        [Fact]
        public void ExternalPropertyMockWorksFine()
        {
            var calendarFake = Fake.For<Calendar>(Calendar.GetTimeZone())
                .Setup((DateCalculator dc) => dc.UtcDate)
                .ReachableWith(c => c.CalculateUtcDate())
                .Returns(_currentDate);

            Assert.Equal(_currentDate, calendarFake.Execute(c => c.CalculateUtcDate()));
        }

        [Fact]
        public void StaticPropertyMockWorksFine()
        {
            var calendarFake = Fake.For<Calendar>(Calendar.GetTimeZone())
                .Setup(() => DateTime.Now)
                .ReachableWith(c => c.GetDate())
                .Returns(_currentDate);

            var expectedDate = new DateTime(2016, 8, 8, 12, 00, 00);
            Assert.Equal(expectedDate, calendarFake.Execute(c => c.GetDate()));
        }

        [Fact]
        public void InstanceMethodMockWorksFine()
        {
            var calendarFake = Fake.For<Calendar>(Calendar.GetTimeZone())
                .Setup((Calendar c) => c.GetCurrentDate())
                .ReachableWith(c => c.GetDateWithInnerCall())
                .Returns(_currentDate);

            var expectedDate = new DateTime(2016, 8, 8, 12, 00, 00);
            Assert.Equal(expectedDate, calendarFake.Execute(c => c.GetDateWithInnerCall()));
        }

        [Fact]
        public void ExternalMethodMockWorksFine()
        {
            var offset = 1;
            var calendarFake = Fake.For<Calendar>(Calendar.GetTimeZone())
                .Setup((DateCalculator dc) => dc.AddHours(_currentDate, offset))
                .ReachableWith(c => c.GetDateWithOffset(offset))
                .Returns(_currentDate);

            Assert.Equal(_currentDate, calendarFake.Execute(c => c.GetDateWithOffset(offset)));
        }

        [Fact]
        public void StaticMethodMockWorksFine()
        {
            var calendarFake = Fake.For<Calendar>(Calendar.GetTimeZone())
                .Setup(() => Calendar.GetTimeZone())
                .ReachableWith(c => c.GetTimeZoneInfo())
                .Returns(null);

            Assert.Null(calendarFake.Execute(c => c.GetTimeZoneInfo()));
        }

        [Fact]
        public void MultipleReachableWithWorksFine()
        {
            var calendarFake = Fake.For<Calendar>(Calendar.GetTimeZone())
                .Setup(() => DateTime.Now)
                .ReachableWith(c => c.GetDate())
                .ReachableWith(c => c.GetDateWithInnerCall())
                .Returns(_currentDate);

            var expectedDate = new DateTime(2016, 8, 8, 12, 00, 00);
            Assert.Equal(expectedDate, calendarFake.Execute(c => c.GetDate()));
            Assert.Equal(expectedDate, calendarFake.Execute(c => c.GetDateWithInnerCall()));
        }

        [Fact]
        public void InnerCallOfMockedMethodWorksFine()
        {
            var calendarFake = Fake.For<Calendar>(Calendar.GetTimeZone())
                .Setup(() => DateTime.Now)
                .ReachableWith(c => c.GetDateWithInnerCall())
                .Returns(_currentDate);

            var expectedDate = new DateTime(2016, 8, 8, 12, 00, 00);
            Assert.Equal(expectedDate, calendarFake.Execute(c => c.GetDateWithInnerCall()));
        }

        [Fact]
        public void PropertyCallWorksFine()
        {
            var calendarFake = Fake.For<Calendar>(Calendar.GetTimeZone())
                .Setup(() => DateTime.UtcNow)
                .ReachableWith(c => c.UtcDate)
                .Returns(_currentDate);
            Assert.Equal(_currentDate, calendarFake.Execute(c => c.UtcDate));
        }
    }
}
