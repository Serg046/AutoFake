using System;

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

        public DateTime GetDateWithOffset(int offset)
        {
            return new DateCalculator().AddHours(GetCurrentDate(), offset);
        }

        public DateTime GetCurrentDate() => DateTime.Now;
    }

    public class DateCalculator
    {
        public DateTime AddHours(DateTime date, int hours) => date.AddHours(hours);
    }
}
