using System;
using System.Linq;

namespace IntegrationTests
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

    public class VerifiableAnalyzer
    {
        public bool IsAnalyzeExecuted;
        private readonly Calculator _calculator = new Calculator();

        static VerifiableAnalyzer()
        {
            Console.WriteLine(1);
        }

        public int GetAnalyzeValue(int a, int b)
        {
            var currentValue = _calculator.Add(a, b);
            return ProcessValue(currentValue, a, b);
        }

        public void Analyze(int a, int b)
        {
            IsAnalyzeExecuted = true;
            GetAnalyzeValue(a, b);
        }

        private int ProcessValue(int currentValue, int a, int b)
        {
            return PrepareValue(currentValue + _calculator.Add(a, b), a, b);
        }

        private int PrepareValue(int currentValue, int a, int b)
        {
            return currentValue + _calculator.Add(a, b);
        }

        public void AnalyzeAndWrite(int a, int b)
        {
            GetAnalyzeValue(a, b);
            WriteValues(a, b);
        }

        public void WriteValues(int a, int b)
        {
            Console.WriteLine(a + " " + b);
        }

        public void AnalyzeTwoWays(int a, int b)
        {
            if (a > 0)
                Analyze(a, b);
            else
                Analyze(0, 0);
        }
    }

    public class Calculator
    {
        public int Add(int a, int b) => a + b;
    }
}
