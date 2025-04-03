using AutoFake.Abstractions;
using AutoFake.Abstractions.Setup;
using AutoFake.Abstractions.Setup.Configurations;
using AutoFake.Abstractions.Setup.Patches;
using AutoFake.Setup;
using AutoFake.Setup.Configurations;
using AutoFake.Setup.Patches;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Pure.DI;
using IServiceProvider = AutoFake.Abstractions.IServiceProvider;

namespace AutoFake;

internal partial class DefaultCompositionRoot : IServiceProvider, IPatchConfigurationFactory
{
    private void Setup()
    {
        DI.Setup()
            .Bind<MethodReference>().To<MethodReference>("patch")
            .RootBind<Func<MethodReference, IPatchMember>>().To<Func<MethodReference, IPatchMember>>(ctx => patch =>
            {
                ctx.Inject<PatchMethod>(out var member);
                return member;
            })
            .Bind<FieldReference>().To<FieldReference>("patch")
            .RootBind<Func<FieldReference, IPatchMember>>().To<Func<FieldReference, IPatchMember>>(ctx => patch =>
            {
                ctx.Inject<PatchField>(out var member);
                return member;
            })
            .Bind<MethodDefinition>().To<MethodDefinition>("entryPoint")
            .Bind<MethodDefinition>("patchCallback").To<MethodDefinition>("patchCallback")
            .Bind<IPatchMember>().To<IPatchMember>("patchMember")
            .RootBind<Func<MethodDefinition, MethodDefinition, IPatchMember, IPatch>>().To<Func<MethodDefinition, MethodDefinition, IPatchMember, IPatch>>(ctx =>
                (entryPoint, patchCallback, patchMember) =>
                {
                    ctx.Inject<ReplacePatch>(out var patchCfg);
                    return patchCfg;
                })
            .Bind<MethodBody>().To<MethodBody>("method")
            .Bind<Instruction>().To<Instruction>("instruction")
            .RootBind<Func<MethodBody, Instruction, IEmitter>>().To<Func<MethodBody, Instruction, IEmitter>>(ctx =>
                (method, instruction) =>
                {
                    ctx.Inject<Emitter>(out var emitter);
                    return emitter;
                })
            .Bind<IEmitter>().To<IEmitter>("emitter")
            .RootBind<Func<IEmitter,IByteCodeProcessor>>().To<Func<IEmitter,IByteCodeProcessor>>(ctx => emitter =>
            {
                ctx.Inject<ByteCodeProcessor>(out var processor);
                return processor;
            })
            .Bind<IPatch>().To<IPatch>("patch")
#pragma warning disable DIW003
            .RootBind<Func<IPatch,IReplacePatchConfiguration<TT>>>("CreateReplacePatchConfiguration").To<Func<IPatch,IReplacePatchConfiguration<TT>>>(ctx => patch =>
            {
                ctx.Inject<ReplacePatchConfiguration<TT>>(out var cfg);
                return cfg;
            })
#pragma warning restore DIW003

            .RootBind<IPatchConfigurationFactory>().To<IPatchConfigurationFactory>(_ => this)
            .RootBind<IFakeCallback>().To<FakeCallback>()
            .RootBind<IPatchConfiguration>().To<PatchConfiguration>()
            .RootBind<IMemberNamePool>().As(Lifetime.Singleton).To<MemberNamePool>()
            .RootBind<IPatchCollection>().As(Lifetime.Singleton).To<PatchCollection>();
    }
}

public partial class CompositionRoot : ICompositionRoot, IPatchConfigurationFactory
{
    private readonly Dictionary<Type, Delegate> _additionalRegistrations = new();
    
    private void SetupExtended()
    {
        DI.Setup()
            .DependsOn(nameof(DefaultCompositionRoot))
            .Hint(Hint.OnDependencyInjection, "On");
    }

    public void ReplaceService<T>(Func<ICompositionRoot, T> factory)
    {
        var type = typeof(T);
        _additionalRegistrations.Remove(type);
        _additionalRegistrations.Add(type, factory);
    }
        
    private partial T OnDependencyInjection<T>(in T value, object? tag, Lifetime lifetime)
    {
        if (_additionalRegistrations.TryGetValue(typeof(T), out var factory))
        {
            if (factory is not Func<ICompositionRoot, T> typedFactory)
            {
                throw new InvalidOperationException("The service factory is invalid");
            }

            return typedFactory(this);
        }
        
        return value;
    }
}