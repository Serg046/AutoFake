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
                .Returns(_currentDate);

            Assert.Equal(_currentDate, calendarFake.Execute(c => c.GetUtcDate()));
        }

        [Fact]
        public void ExternalPropertyMockWorksFine()
        {
            var calendarFake = Fake.For<Calendar>(Calendar.GetTimeZone())
                .Setup((DateCalculator dc) => dc.UtcDate)
                .Returns(_currentDate);

            Assert.Equal(_currentDate, calendarFake.Execute(c => c.CalculateUtcDate()));
        }

        [Fact]
        public void StaticPropertyMockWorksFine()
        {
            var calendarFake = Fake.For<Calendar>(Calendar.GetTimeZone())
                .Setup(() => DateTime.Now)
                .Returns(_currentDate);

            var expectedDate = TimeZoneInfo.ConvertTime(_currentDate, Calendar.GetTimeZone());
            Assert.Equal(expectedDate, calendarFake.Execute(c => c.GetConvertedDate()));
        }

        [Fact]
        public void InstanceMethodMockWorksFine()
        {
            var calendarFake = Fake.For<Calendar>(Calendar.GetTimeZone())
                .Setup((Calendar c) => c.GetCurrentDate())
                .Returns(_currentDate);

            var expectedDate = TimeZoneInfo.ConvertTime(_currentDate, Calendar.GetTimeZone());
            Assert.Equal(expectedDate, calendarFake.Execute(c => c.GetConvertedDateWithInnerCall()));
        }

        [Fact]
        public void ExternalMethodMockWorksFine()
        {
            var offset = 1;
            var calendarFake = Fake.For<Calendar>(Calendar.GetTimeZone())
                .Setup((DateCalculator dc) => dc.AddHours(_currentDate, offset))
                .Returns(_currentDate);

            Assert.Equal(_currentDate, calendarFake.Execute(c => c.GetDateWithOffset(offset)));
        }

        [Fact]
        public void StaticMethodMockWorksFine()
        {
            var calendarFake = Fake.For<Calendar>(Calendar.GetTimeZone())
                .Setup(() => Calendar.GetTimeZone())
                .Returns(null);

            Assert.Null(calendarFake.Execute(c => c.GetTimeZoneInfo()));
        }

        [Fact]
        public void InnerCallOfMockedMethodWorksFine()
        {
            var calendarFake = Fake.For<Calendar>(Calendar.GetTimeZone())
                .Setup(() => DateTime.Now)
                .Returns(_currentDate);

            var expectedDate = TimeZoneInfo.ConvertTime(_currentDate, Calendar.GetTimeZone());
            Assert.Equal(expectedDate, calendarFake.Execute(c => c.GetConvertedDateWithInnerCall()));
        }

        [Fact]
        public void PropertyCallWorksFine()
        {
            var calendarFake = Fake.For<Calendar>(Calendar.GetTimeZone())
                .Setup(() => DateTime.UtcNow)
                .Returns(_currentDate);
            Assert.Equal(_currentDate, calendarFake.Execute(c => c.UtcDate));
        }

        [Fact]
        public void InjectingDoesNotCorruptMethodState()
        {
            var todayDate = new DateTime(2016, 8, 11);
            var calendarFake = Fake.For<Calendar>(Calendar.GetTimeZone())
                .Setup((Calendar c) => c.GetCurrentDate())
                .Returns(todayDate);
            Assert.Equal(new DateTime(2016, 8, 12), calendarFake.Execute(c => c.GetNextWorkingDate()));

            calendarFake = Fake.For<Calendar>(Calendar.GetTimeZone())
                .Setup((Calendar c) => c.GetCurrentDate())
                .Returns(todayDate.AddDays(1));
            Assert.Equal(new DateTime(2016, 8, 15), calendarFake.Execute(c => c.GetNextWorkingDate()));
        }

        [Fact]
        public void VerifiableWorksFine()
        {
            var incorrectDate = new DateTime(2016, 5, 5);

            var offset = 1;
            var calendarFake = Fake.For<Calendar>(Calendar.GetTimeZone());
            calendarFake.Setup((DateCalculator dc) => dc.AddHours(incorrectDate, offset))
                .Verifiable()
                .Returns(_currentDate);

            Assert.Throws<InvalidOperationException>(() => calendarFake.Execute(c => c.GetDateWithOffset(offset)));

            calendarFake.Setup(() => DateTime.Now).Returns(incorrectDate);
            calendarFake.Execute(c => c.GetDateWithOffset(offset));
        }

        [Fact]
        public void ExpectedCallsCountWorksFine()
        {
            var todayDate = new DateTime(2016, 8, 11);
            var calendarFake = Fake.For<Calendar>(Calendar.GetTimeZone())
                .Setup(() => DateTime.Now)
                .ExpectedCallsCount(1)
                .Returns(todayDate);

            Assert.Throws<InvalidOperationException>(() => calendarFake.Execute(c => c.GetNextWorkingDate()));

            calendarFake = Fake.For<Calendar>(Calendar.GetTimeZone())
                .Setup(() => DateTime.Now)
                .ExpectedCallsCount(2)
                .Returns(todayDate);

            Assert.Equal(new DateTime(2016, 8, 12), calendarFake.Execute(c => c.GetNextWorkingDate()));
        }

        [Fact]
        public void VerifiableChecksAllCallsInCurrentAssembly()
        {
            var fake = Fake.For<VerifiableAnalyzer>()
                .Setup((Calculator calc) => calc.Add(1, 2))
                .Verifiable()
                .ExpectedCallsCount(3)
                .Returns(1);
            fake.Execute(f => f.GetAnalyzeValue(1, 2));
        }

        [Fact]
        public void VoidCallWorksFine()
        {
            var fake = Fake.For<VerifiableAnalyzer>()
                .Setup((Calculator calc) => calc.Add(1, 2))
                .Verifiable()
                .ExpectedCallsCount(3)
                .Returns(1);
            fake.Execute(f => f.Analyze(1, 2));
        }

        [Fact]
        public void SetupWithoutExpectedReturnValueWorksFine()
        {
            var fake = Fake.For<VerifiableAnalyzer>();
            fake.Setup((Calculator calc) => calc.Add(1, 2))
                .Verifiable()
                .ExpectedCallsCount(3)
                .Returns(1);
            fake.Setup((VerifiableAnalyzer v) => v.WriteValues(1, 2))
                .Verifiable()
                .ExpectedCallsCount(1)
                .Void();
            fake.Execute(f => f.AnalyzeAndWrite(1, 2));
        }
    }
}
