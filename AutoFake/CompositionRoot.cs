using System.Reflection;
using AutoFake.Abstractions;
using AutoFake.Abstractions.Setup;
using AutoFake.Abstractions.Setup.Configurations;
using AutoFake.Abstractions.Setup.Patches;
using AutoFake.Setup;
using AutoFake.Setup.Configurations;
using AutoFake.Setup.Patches;
using Mono.Cecil;
using Pure.DI;
using static Pure.DI.Hint;
using static Pure.DI.Lifetime;
using static Pure.DI.Tag;
using IServiceProvider = AutoFake.Abstractions.IServiceProvider;

namespace AutoFake;

internal partial class DefaultCompositionRoot : IServiceProvider, IPatchConfigurationFactory
{
    private void Setup()
    {
        DI.Setup()
            .Hint(ResolveMethodName, nameof(IServiceProvider.Resolve))
            .Bind(Method).To<PatchMethod>()
            .Bind(Field).To<PatchField>()
            .Bind<MethodDefinition>(PatchCallback).To<MethodDefinition>("patchCallback")
            .Bind<Func<string, MethodDefinition, MethodDefinition, IPatchMember, IPatch>>().To<Func<string, MethodDefinition, MethodDefinition, IPatchMember, IPatch>>(ctx =>
                (patchKey, entryPoint, patchCallback, patchMember) =>
                {
                    ctx.Override(patchKey);
                    ctx.Override(entryPoint);
                    ctx.Override(patchMember);
                    ctx.Inject<ReplacePatch>(out var patchCfg);
                    return patchCfg;
                })
            .Bind().To<Emitter>()
            .Bind().To<ByteCodeProcessor>()
            .Bind().To<IPatchConfigurationFactory>(_ => this)
            .Bind().As(Singleton).To<MemberNamePool>()
            .Bind().To<PatchConfiguration>()
            .Bind().To<ReplacePatchConfiguration<TT>>()

            .RootBind<IPatchCollection>().As(Singleton).To<PatchCollection>()
            .RootBind<IFakeCallback>().To<FakeCallback>()
            .RootBind<Func<MethodBase,IPatchConfiguration>>().To<Func<MethodBase,IPatchConfiguration>>(ctx => entryPoint =>
            {
                ctx.Override(entryPoint);
                ctx.Inject<PatchConfiguration>(out var cfg);
                return cfg;
            })
#pragma warning disable DIW003 // The root can be used from the method only
            .Root<Func<IPatch,IReplacePatchConfiguration<TT>>>(nameof(ReplacePatchConfigurationFactory));
#pragma warning disable DIW003
    }

    public IReplacePatchConfiguration<TReturn> CreateReplacePatchConfiguration<TReturn>(IPatch patch)
    {
        return ReplacePatchConfigurationFactory<TReturn>()(patch);
    }
}

public partial class CompositionRoot : ICompositionRoot, IPatchConfigurationFactory
{
    private readonly Dictionary<Type, Delegate> _additionalRegistrations = new();

    private void SetupExtended()
    {
        DI.Setup()
            .DependsOn(nameof(DefaultCompositionRoot))
            .Hint(Hint.OnDependencyInjection, Name.On);
    }

    public void ReplaceService<T>(Func<ICompositionRoot, T> factory)
    {
        var type = typeof(T);
        _additionalRegistrations.Remove(type);
        _additionalRegistrations.Add(type, factory);
    }
        
    private partial T OnDependencyInjection<T>(in T value, object? tag, Lifetime lifetime)
    {
        if (!_additionalRegistrations.TryGetValue(typeof(T), out var factory))
        {
            return value;
        }

        if (factory is not Func<ICompositionRoot, T> typedFactory)
        {
            throw new InvalidOperationException("The service factory is invalid");
        }

        return typedFactory(this);

    }

    public IReplacePatchConfiguration<TReturn> CreateReplacePatchConfiguration<TReturn>(IPatch patch)
    {
        return ReplacePatchConfigurationFactory<TReturn>()(patch);
    }
}