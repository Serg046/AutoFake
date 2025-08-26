using AutoFake.xUnit;
using Shouldly;

namespace AutoFake.Tests.PatchMemberTests;

public class PatchFieldTests
{
    private readonly System _system = new();
    
    [AutoFakeFact]
    public void When_instance_field_Should_patch()
    {
        var date = new DateTime(2025, 8, 24);

        Fake.Patch((System s) => s.CallInstanceField())
            .Replace((Impl i) => i.InstanceField)
            .Return(date);
        
        _system.CallInstanceField().ShouldBe(date);
    }
    
    [AutoFakeFact]
    public void When_static_field_Should_patch()
    {
        var date = new DateTime(2025, 8, 24);

        Fake.Patch((System s) => s.CallStaticField())
            .Replace(() => Impl.StaticField)
            .Return(date);
        
        _system.CallStaticField().ShouldBe(date);
    }
    
    [AutoFakeFact]
    public void When_generic_field_Should_patch()
    {
        const int setup = 7;
        Fake.Patch((System s) => s.CallGenericField())
            .Replace((Impl<int> i) => i.GenericField)
            .Return(setup);

        _system.CallGenericField().ShouldBe(setup);
    }

    [AutoFakeFact]
    public void When_enumerable_field_Should_patch()
    {
        var date = new DateTime(2025, 8, 24);

        Fake.Patch((System s) => s.CallEnumerableField())
            .Replace((Impl i) => i.EnumerableField)
            .Return([date]);

        _system.CallEnumerableField().ShouldBe([date]);
    }
    
    private class System
    {
        private readonly Impl _impl = new();

        public DateTime CallInstanceField() => _impl.InstanceField;
        public DateTime CallStaticField() => Impl.StaticField;
        public int CallGenericField() => new Impl<int>().GenericField;
        public IEnumerable<DateTime> CallEnumerableField() => _impl.EnumerableField;
    }
    
    private class Impl
    {
        public readonly DateTime InstanceField = DateTime.Now;
        public static readonly DateTime StaticField = DateTime.Now;
        public readonly IEnumerable<DateTime> EnumerableField = [DateTime.Now];
    }

    private class Impl<T1>
    {
        public readonly T1 GenericField = Extensions.CreateDefault<T1>();
    }
}