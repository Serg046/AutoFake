using AutoFake;
using System;
using System.Linq;
using Xunit;

namespace UnitTests
{
    public class FakeSetupTests
    {
        [Fact]
        public void Returns_WithoutReachableWith_ThrowsException()
        {
            var currentDate = new DateTime(2016, 8, 8, 15, 00, 00, DateTimeKind.Local);

            var timeZone = TimeZoneInfo.GetSystemTimeZones().Single(t => t.Id == "UTC");
            var setup = Fake.For<Calendar>(timeZone)
                .Setup(() => DateTime.Now);

            Assert.Throws<InvalidOperationException>(() => setup.Returns(currentDate));
        }
    }
}
