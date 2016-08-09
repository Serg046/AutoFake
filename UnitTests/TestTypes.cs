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

        public DateTime GetDate()
        {
            return TimeZoneInfo.ConvertTime(DateTime.Now, _timeZone);
        }

        public DateTime GetDateWithInnerCall()
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
    }

    public class DateCalculator
    {
        private readonly DateTime _currentDate;

        public DateCalculator(DateTime currentDate)
        {
            _currentDate = currentDate;
        }

        public DateTime AddHours(DateTime date, int hours) => date.AddHours(hours);

        public DateTime UtcDate => _currentDate.ToUniversalTime();
    }
}
