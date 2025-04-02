using System.Text;
using AutoFake.xUnit;
using Shouldly;

namespace AutoFake.Tests;

public class Arguments
{
    [AutoFakeFact]
    public void Test1()
    {
        var date = new DateTime(2024, 3, 13);

        Fake.Patch((SystemUnderTest sut) => sut.GetTomorrowDate())
            .Replace((DateTime dt) => dt.AddDays(1))
            .Return(date);

        var sut = new SystemUnderTest();
        sut.GetTomorrowDate().ShouldBe(date);
    }
    
    [AutoFakeFact]
    public void Test2()
    {
        var str = "modified";

        Fake.Patch((SystemUnderTest sut) => sut.GetString())
            .Replace((StringBuilder sb) => sb.Append("test", 1, 2))
            .Return(new StringBuilder("modified"));

        var sut = new SystemUnderTest();
        sut.GetString().ShouldBe(str);
    }
    
    private class SystemUnderTest
    {
        public DateTime GetTomorrowDate() => DateTime.Now.AddDays(1);
        public string GetString() => new StringBuilder("base").Append("test", 1, 2).ToString();
    }
}