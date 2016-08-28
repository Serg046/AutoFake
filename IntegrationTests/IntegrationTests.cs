using AutoFake;
using System;
using AutoFake.Exceptions;
using Xunit;

namespace UnitTests
{
    public class IntegrationTests
    {
        private readonly DateTime _currentDate = new DateTime(2016, 8, 8, 15, 00, 00, DateTimeKind.Local);

        [Fact]
        public void InstancePropertyMockWorksFine()
        {
            var calendarFake = new Fake<Calendar>(Calendar.GetTimeZone());
            calendarFake.Setup((Calendar c) => c.UtcDate).Returns(_currentDate);

            Assert.Equal(_currentDate, calendarFake.Execute(c => c.GetUtcDate()));
        }

        [Fact]
        public void ExternalPropertyMockWorksFine()
        {
            var calendarFake = new Fake<Calendar>(Calendar.GetTimeZone());
            calendarFake.Setup((DateCalculator dc) => dc.UtcDate).Returns(_currentDate);

            Assert.Equal(_currentDate, calendarFake.Execute(c => c.CalculateUtcDate()));
        }

        [Fact]
        public void StaticPropertyMockWorksFine()
        {
            var calendarFake = new Fake<Calendar>(Calendar.GetTimeZone());
            calendarFake.Setup(() => DateTime.Now).Returns(_currentDate);

            var expectedDate = TimeZoneInfo.ConvertTime(_currentDate, Calendar.GetTimeZone());
            Assert.Equal(expectedDate, calendarFake.Execute(c => c.GetConvertedDate()));
        }

        [Fact]
        public void InstanceMethodMockWorksFine()
        {
            var calendarFake = new Fake<Calendar>(Calendar.GetTimeZone());
            calendarFake.Setup((Calendar c) => c.GetCurrentDate()).Returns(_currentDate);

            var expectedDate = TimeZoneInfo.ConvertTime(_currentDate, Calendar.GetTimeZone());
            Assert.Equal(expectedDate, calendarFake.Execute(c => c.GetConvertedDateWithInnerCall()));
        }

        [Fact]
        public void ExternalMethodMockWorksFine()
        {
            var offset = 1;
            var calendarFake = new Fake<Calendar>(Calendar.GetTimeZone());
            calendarFake.Setup((DateCalculator dc) => dc.AddHours(_currentDate, offset)).Returns(_currentDate);

            Assert.Equal(_currentDate, calendarFake.Execute(c => c.GetDateWithOffset(offset)));
        }

        [Fact]
        public void StaticMethodMockWorksFine()
        {
            var calendarFake = new Fake<Calendar>(Calendar.GetTimeZone());
            calendarFake.Setup(() => Calendar.GetTimeZone()).Returns(null);

            Assert.Null(calendarFake.Execute(c => c.GetTimeZoneInfo()));
        }

        [Fact]
        public void InnerCallOfMockedMethodWorksFine()
        {
            var calendarFake = new Fake<Calendar>(Calendar.GetTimeZone());
            calendarFake.Setup(() => DateTime.Now).Returns(_currentDate);

            var expectedDate = TimeZoneInfo.ConvertTime(_currentDate, Calendar.GetTimeZone());
            Assert.Equal(expectedDate, calendarFake.Execute(c => c.GetConvertedDateWithInnerCall()));
        }

        [Fact]
        public void PropertyCallWorksFine()
        {
            var calendarFake = new Fake<Calendar>(Calendar.GetTimeZone());
            calendarFake.Setup(() => DateTime.UtcNow).Returns(_currentDate);

            Assert.Equal(_currentDate, calendarFake.Execute(c => c.UtcDate));
        }

        [Fact]
        public void InjectingDoesNotCorruptMethodState()
        {
            var todayDate = new DateTime(2016, 8, 11);
            var calendarFake = new Fake<Calendar>(Calendar.GetTimeZone());
            calendarFake.Setup((Calendar c) => c.GetCurrentDate()).Returns(todayDate);

            Assert.Equal(new DateTime(2016, 8, 12), calendarFake.Execute(c => c.GetNextWorkingDate()));

            calendarFake = new Fake<Calendar>(Calendar.GetTimeZone());
            calendarFake.Setup((Calendar c) => c.GetCurrentDate()).Returns(todayDate.AddDays(1));

            Assert.Equal(new DateTime(2016, 8, 15), calendarFake.Execute(c => c.GetNextWorkingDate()));
        }

        [Fact]
        public void VerifiableWorksFine()
        {
            var incorrectDate = new DateTime(2016, 5, 5);

            var offset = 1;
            var calendarFake = new Fake<Calendar>(Calendar.GetTimeZone());
            calendarFake.Setup((DateCalculator dc) => dc.AddHours(incorrectDate, offset))
                .Verifiable()
                .Returns(_currentDate);

            Assert.Throws<VerifiableException>(() => calendarFake.Execute(c => c.GetDateWithOffset(offset)));

            calendarFake.Setup(() => DateTime.Now).Returns(incorrectDate);
            calendarFake.Execute(c => c.GetDateWithOffset(offset));
        }

        [Fact]
        public void ExpectedCallsCountWorksFine()
        {
            var todayDate = new DateTime(2016, 8, 11);
            var calendarFake = new Fake<Calendar>(Calendar.GetTimeZone());
            calendarFake.Setup(() => DateTime.Now)
                .ExpectedCallsCount(1)
                .Returns(todayDate);

            Assert.Throws<ExpectedCallsException>(() => calendarFake.Execute(c => c.GetNextWorkingDate()));

            calendarFake = new Fake<Calendar>(Calendar.GetTimeZone());
            calendarFake.Setup(() => DateTime.Now)
                .ExpectedCallsCount(2)
                .Returns(todayDate);

            Assert.Equal(new DateTime(2016, 8, 12), calendarFake.Execute(c => c.GetNextWorkingDate()));
        }

        [Fact]
        public void VerifiableChecksAllCallsInCurrentAssembly()
        {
            var fake = new Fake<VerifiableAnalyzer>();
            fake.Setup((Calculator calc) => calc.Add(1, 2))
                .Verifiable()
                .ExpectedCallsCount(3)
                .Returns(1);

            fake.Execute(f => f.GetAnalyzeValue(1, 2));
        }

        [Fact]
        public void VoidCallWorksFine()
        {
            var fake = new Fake<VerifiableAnalyzer>();
            fake.Setup((Calculator calc) => calc.Add(1, 2))
                .Verifiable()
                .ExpectedCallsCount(3)
                .Returns(1);

            fake.Execute(f => f.Analyze(1, 2));
        }

        [Fact]
        public void SetupWithoutExpectedReturnValueWorksFine()
        {
            var fake = new Fake<VerifiableAnalyzer>();
            fake.Setup((Calculator calc) => calc.Add(1, 2))
                .Verifiable()
                .ExpectedCallsCount(3)
                .Returns(1);
            fake.Setup((VerifiableAnalyzer v) => v.WriteValues(1, 2))
                .Verifiable()
                .ExpectedCallsCount(1);

            fake.Execute(f => f.AnalyzeAndWrite(1, 2));
        }
    }
}
