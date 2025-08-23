using AutoFake.xUnit;
using Shouldly;

namespace AutoFake.Tests.PatchMemberTests;

public class PatchPropertyTests
{
    private readonly System _system = new();
    
    [AutoFakeFact]
    public void When_instance_property_Should_patch()
    {
        var date = new DateTime(2025, 8, 24);

        Fake.Patch((System s) => s.CallInstanceProperty())
            .Replace((Impl i) => i.InstanceProperty)
            .Return(date);
        
        _system.CallInstanceProperty().ShouldBe(date);
    }
    
    [AutoFakeFact]
    public void When_static_property_Should_patch()
    {
        var date = new DateTime(2025, 8, 24);

        Fake.Patch((System s) => s.CallStaticProperty())
            .Replace(() => Impl.StaticProperty)
            .Return(date);
        
        _system.CallStaticProperty().ShouldBe(date);
    }
    
    [AutoFakeFact]
    public void When_generic_property_Should_patch()
    {
        const int setup = 7;
        Fake.Patch((System s) => s.CallGenericProperty())
            .Replace((Impl<int> i) => i.GenericProperty)
            .Return(setup);

        _system.CallGenericProperty().ShouldBe(setup);
    }

    [AutoFakeFact]
    public void When_enumerable_property_Should_patch()
    {
        var date = new DateTime(2025, 8, 24);

        Fake.Patch((System s) => s.CallEnumerableProperty())
            .Replace((Impl i) => i.EnumerableProperty)
            .Return([date]);

        _system.CallEnumerableProperty().ShouldBe([date]);
    }
    
    public class System
    {
        private readonly Impl _impl = new();

        public DateTime CallInstanceProperty() => _impl.InstanceProperty;
        public DateTime CallStaticProperty() => Impl.StaticProperty;
        public int CallGenericProperty() => new Impl<int>().GenericProperty;
        public IEnumerable<DateTime> CallEnumerableProperty() => _impl.EnumerableProperty;
    }
    
    public class Impl
    {
        public DateTime InstanceProperty => DateTime.Now;
        public static DateTime StaticProperty => DateTime.Now;
        public IEnumerable<DateTime> EnumerableProperty => [DateTime.Now];
    }

    public class Impl<T1>
    {
        public T1 GenericProperty => Extensions.CreateDefault<T1>();
    }
}