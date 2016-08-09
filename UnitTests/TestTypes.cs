using System;
using System.Linq;

namespace UnitTests
{
    public class Calendar
    {
        private readonly TimeZoneInfo _timeZone;

        public Calendar(TimeZoneInfo timeZone)
        {
            _timeZone = timeZone;
        }

        public DateTime UtcDate => DateTime.UtcNow;

        public DateTime GetUtcDate() => UtcDate;

        public DateTime CalculateUtcDate() => new DateCalculator(DateTime.Now).UtcDate;

        public DateTime GetConvertedDate()
        {
            return TimeZoneInfo.ConvertTime(DateTime.Now, _timeZone);
        }

        public DateTime GetConvertedDateWithInnerCall()
        {
            return TimeZoneInfo.ConvertTime(GetCurrentDate(), _timeZone);
        }

        public DateTime GetDateWithOffset(int offset)
        {
            return new DateCalculator(DateTime.Now).AddHours(GetCurrentDate(), offset);
        }

        public DateTime GetCurrentDate() => DateTime.Now;

        public TimeZoneInfo GetTimeZoneInfo() => GetTimeZone();

        public static TimeZoneInfo GetTimeZone()
            => TimeZoneInfo.GetSystemTimeZones().Single(t => t.Id == "UTC");

        public DateTime GetNextWorkingDate()
        {
            var calculator = new DateCalculator();
            var date = GetCurrentDate();
            var offset = calculator.GetDaysCountUntilMonday(date);
            return date.AddDays(offset).Date;
        }
    }

    public class DateCalculator
    {
        private readonly DateTime _currentDate;

        public DateCalculator()
        {
            _currentDate = DateTime.Now;
        }

        public DateCalculator(DateTime currentDate)
        {
            _currentDate = currentDate;
        }

        public DateTime AddHours(DateTime date, int hours) => date.AddHours(hours);

        public DateTime UtcDate => _currentDate.ToUniversalTime();

        public int GetDaysCountUntilMonday(DateTime current)
            => current.DayOfWeek == DayOfWeek.Friday ? 3 : 1;
    }
}
