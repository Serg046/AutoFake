using AutoFake.xUnit;
using Shouldly;

namespace AutoFake.Tests.PatchMemberTests;

public class PatchMethodTests
{
    private readonly System _system = new();
    
    [AutoFakeFact]
    public void When_instance_method_Should_patch()
    {
        var date = new DateTime(2025, 8, 17);

        Fake.Patch((System s) => s.CallInstanceMethod())
            .Replace((Impl i) => i.InstanceMethod())
            .Return(date);
        
        _system.CallInstanceMethod().ShouldBe(date);
    }
    
    [AutoFakeFact]
    public void When_static_method_Should_patch()
    {
        var date = new DateTime(2025, 8, 17);

        Fake.Patch((System s) => s.CallStaticMethod())
            .Replace(Impl.StaticMethod)
            .Return(date);
        
        _system.CallStaticMethod().ShouldBe(date);
    }
    
    [AutoFakeFact]
    public void When_parameterized_method_Should_patch()
    {
        var date = new DateTime(2025, 8, 17);

        Fake.Patch((System s) => s.CallParameterizedMethod(1))
            .Replace((Impl i) => i.ParameterizedMethod(1))
            .Return(date);
        
        _system.CallParameterizedMethod(1).ShouldBe(date);
    }
    
    /* TODO: [AutoFakeFact]
    public void When_void_method_Should_patch()
    {
        var date = new DateTime(2025, 8, 17);

        Fake.Patch((System s) => s.CallVoidMethod())
            .Remove((Impl i) => i.VoidMethod())
            .Return(date);
        //
    }*/
    
    /*[AutoFakeFact]
    public void When_generic_method_Should_patch()
    {
        var setup = (2, "3");
        Fake.Patch((System s) => s.CallGenericMethod())
            .Replace((Impl<int> i) => i.GenericMethod<string>())
            .Return(setup);

        _system.CallGenericMethod().ShouldBe(setup);
    }*/
    
    [AutoFakeFact]
    public void When_overloaded_method_Should_patch()
    {
        var date1 = new DateTime(2025, 8, 17);
        var date2 = new DateTime(2025, 8, 18);

        Fake.Patch((System s) => s.CallOverloadedMethod())
            .Replace((Impl i) => i.OverloadedMethod())
            .Return(date1);
        Fake.Patch((System s) => s.CallOverloadedMethod())
            .Replace((Impl i) => i.OverloadedMethod(1))
            .Return(date2);
        
        _system.CallOverloadedMethod().ShouldBe(date2);
    }
    
    /* TODO: [AutoFakeFact]
    public async Task When_async_method_Should_patch()
    {
        var date = new DateTime(2025, 8, 17);

        Fake.Patch((System s) => s.CallAsyncMethod())
            .Replace((Impl i) => i.AsyncMethod())
            .Return(Task.FromResult(date));

        var actualDate = await _system.CallAsyncMethod();
        actualDate.ShouldBe(date);
    }*/
    
    /* TODO: [AutoFakeFact]
    public void When_params_method_Should_patch()
    {
        var date = new DateTime(2025, 8, 17);

        Fake.Patch((System s) => s.CallParamsMethod())
            .Replace((Impl i) => i.ParamsMethod(1, 2, 3))
            .Return(date);

        _system.CallParamsMethod().ShouldBe(date);
    }*/
    
    [AutoFakeFact]
    public void When_enumerable_method_Should_patch()
    {
        var date = new DateTime(2025, 8, 17);

        Fake.Patch((System s) => s.CallEnumerableMethod())
            .Replace((Impl i) => i.EnumerableMethod())
            .Return([date]);

        _system.CallEnumerableMethod().ShouldBe([date]);
    }
    
    /* TODO: [AutoFakeFact]
    public async Task When_async_enumerable_method_Should_patch()
    {
        var date = new DateTime(2025, 8, 17);

        Fake.Patch((System s) => s.CallAsyncEnumerableMethod())
            .Replace((Impl i) => i.AsyncEnumerableMethod())
            .Return(GetEnumerable());

        await foreach (var actualDate in _system.CallAsyncEnumerableMethod())
        {
            actualDate.ShouldBe(date);
        }

        async IAsyncEnumerable<DateTime> GetEnumerable()
        {
            await Task.Yield();
            yield return date;
        }
    }*/
    
    private class System
    {
        private readonly Impl _impl = new();

        public DateTime CallInstanceMethod() => _impl.InstanceMethod();
        public DateTime CallStaticMethod() => Impl.StaticMethod();
        public DateTime CallParameterizedMethod(int addDays) => _impl.ParameterizedMethod(addDays);
        public void CallVoidMethod() => _impl.VoidMethod();
        public (int, string) CallGenericMethod() => new Impl<int>().GenericMethod<string>();
        public DateTime CallOverloadedMethod() => _impl.OverloadedMethod(1);
        public async Task<DateTime> CallAsyncMethod() => await _impl.AsyncMethod();
        public DateTime CallParamsMethod() => _impl.ParamsMethod(1, 2, 3);
        public IEnumerable<DateTime> CallEnumerableMethod() => _impl.EnumerableMethod();
        
        public async IAsyncEnumerable<DateTime> CallAsyncEnumerableMethod()
        {
            await foreach (var date in _impl.AsyncEnumerableMethod())
            {
                yield return date;
            }
        }
    }
    
    private class Impl
    {
        public DateTime InstanceMethod() => DateTime.Now;
        public static DateTime StaticMethod() => DateTime.Now;
        public DateTime ParameterizedMethod(int addDays) => DateTime.Now.AddDays(addDays);
        public void VoidMethod() => throw new NotImplementedException();
        public DateTime OverloadedMethod() => DateTime.Now;
        public DateTime OverloadedMethod(int addDays) => DateTime.Now.AddDays(addDays);
        public DateTime ParamsMethod(params int[] addDays) => DateTime.Now.AddDays(addDays.Sum());
        
        public async Task<DateTime> AsyncMethod()
        {
            await Task.Yield();
            return DateTime.Now;
        }

        public IEnumerable<DateTime> EnumerableMethod()
        {
            yield return DateTime.Now;
        }
        
        public async IAsyncEnumerable<DateTime> AsyncEnumerableMethod()
        {
            await Task.Yield();
            yield return DateTime.Now;
        }
    }

    private class Impl<T1>
    {
        public (T1, T2) GenericMethod<T2>()
        {
            return (Extensions.CreateDefault<T1>(), Extensions.CreateDefault<T2>());
        }
    }
}