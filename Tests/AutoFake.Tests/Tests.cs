using AutoFake.Abstractions.Setup;
using AutoFake.xUnit;
using Shouldly;
using Xunit;

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

        var sut = new SystemUnderTest();
        sut.GetCurrentDate().ShouldBe(date);
    }
    
    [AutoFakeTheory]
    [InlineData(typeof(SystemUnderTest))]
    public void Test2(Type type)
    {
        Fake.Patch((SystemUnderTest f) => f.GetCurrentDate()).Replace(() => DateTime.Now);
        type.ShouldBe(typeof(SystemUnderTest));
        var patch = Fake.GetServices().Resolve<IPatchCollection>().Single();
        type.GetFields().ShouldContain(f => f.Name == patch.RetValueField.Name);
    }
    
    [AutoFakeTheory]
    [MemberData(nameof(GetSystemUnderTest))]
    public void Test3(SystemUnderTest sut)
    {
        var date = new DateTime(2024, 3, 13);

        Fake.Patch((SystemUnderTest f) => f.GetCurrentDate())
            .Replace(() => DateTime.Now)
            .Return(date);

        sut.GetCurrentDate().ShouldBe(date);
    }
    
    [AutoFakeTheory]
    [ClassData(typeof(SystemUnderTestTestData))]
    public void Test4(SystemUnderTest sut)
    {
        var date = new DateTime(2024, 3, 13);

        Fake.Patch((SystemUnderTest f) => f.GetCurrentDate())
            .Replace(() => DateTime.Now)
            .Return(date);

        sut.GetCurrentDate().ShouldBe(date);
    }

    public static IEnumerable<object[]> GetSystemUnderTest()
    {
        yield return [new SystemUnderTest()];
    }
    
    private class SystemUnderTestTestData : TheoryData<SystemUnderTest>
    {
        public SystemUnderTestTestData()
        {
            Add(new SystemUnderTest());
        }
    }

    public class SystemUnderTest
    {
        public DateTime GetCurrentDate() => DateTime.Now;
    }
}