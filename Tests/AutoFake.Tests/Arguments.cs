using System.Text;
using AutoFake.xUnit;
using Shouldly;
using Xunit;

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
        const string str = "modified";

        Fake.Patch((SystemUnderTest sut) => sut.GetString())
            .Replace((StringBuilder sb) => sb.Append("test", 1, 2))
            .Return(new StringBuilder(str));

        var sut = new SystemUnderTest();
        sut.GetString().ShouldBe(str);
    }
    
    [AutoFakeFact]
    public void Test3()
    {
        const string str = "modified";

        Fake.Patch((SystemUnderTest sut) => sut.GetString())
            .Replace(() => new StringBuilder(Arg.Is<string>(a => a == "base")))
            .Return(new StringBuilder(str));

        var sut = new SystemUnderTest();
        sut.GetString().ShouldBe(str + "es");
    }
    
    [AutoFakeFact]
    public void Test4()
    {
        const string str = "modified";

        Fake.Patch((SystemUnderTest sut) => sut.GetString())
            .Replace((StringBuilder sb) => sb.Append(Arg.Is<string>(a => a == "test"), 1, 2))
            .Return(new StringBuilder(str));

        var sut = new SystemUnderTest();
        sut.GetString().ShouldBe(str);
    }
    
    [Fact]
    public void Test5()
    {
        var act = () => Fake.Run(() =>
        {
            Fake.Patch((SystemUnderTest sut) => sut.GetString())
                .Replace((StringBuilder sb) => sb.Append("test", Arg.Is<int>(a => a == 1), 2))
                .Return(new StringBuilder("modified"));
        });
        act.ShouldThrow<InvalidOperationException>();
    }
    
    [AutoFakeFact]
    public void Test6()
    {
        const string str = "modified";

        Fake.Patch((SystemUnderTest sut) => sut.GetString())
            .Replace((StringBuilder sb) => sb.Append(
                Arg.Is<string>(a => a == "test"),
                Arg.Is<int>(a => a == 1),
                Arg.Is<int>(a => a == 2)))
            .Return(new StringBuilder(str));

        var sut = new SystemUnderTest();
        sut.GetString().ShouldBe(str);
    }
    
    [AutoFakeFact]
    public void Test7()
    {
        const string str = "modified";

        Fake.Patch((SystemUnderTest sut) => sut.GetString())
            .Replace(() => new StringBuilder(Arg.IsAny<string>()))
            .Return(new StringBuilder(str));

        var sut = new SystemUnderTest();
        sut.GetString().ShouldBe(str + "es");
    }
    
    private class SystemUnderTest
    {
        public DateTime GetTomorrowDate() => DateTime.Now.AddDays(1);
        public string GetString() => new StringBuilder("base").Append("test", 1, 2).ToString();
    }
}