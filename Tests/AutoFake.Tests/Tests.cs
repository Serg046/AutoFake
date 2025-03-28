using AutoFake.xUnit;
using Shouldly;

namespace AutoFake.Tests;

public class Tests
{
    [AutoFakeFact]
    public void Test()
    {
        var date = new DateTime(2024, 3, 13);

        Fake.Patch((SystemUnderTest f) => f.GetCurrentDate())
            .Replace(() => DateTime.Now)
            .Return(date);

        var sys = new SystemUnderTest();
        sys.GetCurrentDate().ShouldBe(date);
    }
    
    private class SystemUnderTest
    {
        public DateTime GetCurrentDate() => DateTime.Now;
    }
}