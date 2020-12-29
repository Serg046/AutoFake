using System;
using System.Threading.Tasks;

namespace AutoFake.IntegrationTests.Sut
{
    public class Calendar
    {
        public static DateTime Yesterday => DateTime.Now.AddDays(-1);
        internal Task<DateTime> AddSomeMinutesAsync(DateTime date) => Task.Run(() => AddSomeMinutes(date));
        public static DateTime AddSomeMinutes(DateTime date) => date.AddMinutes(new Random().Next(1, 10));
    }
}
