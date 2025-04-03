using AutoFake.Abstractions.Setup;
using AutoFake.xUnit;
using Shouldly;
using Xunit;

namespace AutoFake.Tests;

public class Members
{
    [AutoFakeFact]
    public void Test1()
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
        patch.RetValueField.ShouldNotBeNull();
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

    [AutoFakeFact]
    public void Test5()
    {
        var date = new DateTime(2024, 3, 13);

        Fake.Patch((SystemUnderTest s) => s.GetDateFromField())
            .Replace((SystemUnderTest s) => s.DateField)
            .Return(date);
        
        var sut = new SystemUnderTest();
        sut.GetDateFromField().ShouldBe(date);
    }
    
    [AutoFakeFact]
    public void Test6()
    {
        var date = new DateTime(2024, 3, 13);

        Fake.Patch((SystemUnderTest s) => s.GetDateFromProperty())
            .Replace((SystemUnderTest s) => s.DateProperty)
            .Return(date);
        
        var sut = new SystemUnderTest();
        sut.GetDateFromProperty().ShouldBe(date);
    }
    
    [AutoFakeFact]
    public void Test7()
    {
        var date = new DateTime(2024, 3, 13);

        Fake.Patch((SystemUnderTest s) => s.DateProperty)
            .Replace(() => DateTime.Now)
            .Return(date);
        
        var sut = new SystemUnderTest();
        sut.DateProperty.ShouldBe(date);
    }
    
    [AutoFakeFact]
    public void Test8()
    {
        var date = new DateTime(2024, 3, 13);

        Fake.Patch(() => new SystemUnderTest())
            .Replace(() => DateTime.Now)
            .Return(date);
        
        var sut = new SystemUnderTest();
        sut.DatePropertyWithInit.ShouldBe(date);
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
        public DateTime DateField = DateTime.Now;
        public DateTime DateProperty => DateTime.Now;
        public DateTime DatePropertyWithInit { get; } = DateTime.Now;
        public DateTime GetCurrentDate() => DateTime.Now;
        public DateTime GetDateFromField() => DateField;
        public DateTime GetDateFromProperty() => DateProperty;
        public DateTime GetDateFromInitializedProperty() => DateProperty;
    }
}