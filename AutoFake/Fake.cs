using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using AutoFake.Abstractions;
using AutoFake.Abstractions.Setup;
using AutoFake.Abstractions.Setup.Configurations;
using Mono.Cecil;
using Mono.Cecil.Cil;
using IServiceProvider = AutoFake.Abstractions.IServiceProvider;
using ModuleDefinition = Mono.Cecil.ModuleDefinition;

namespace AutoFake;

public static class Fake
{
    private static readonly Dictionary<Assembly, IServiceProvider> _compositionRoots = new();
    
    public static void Run(Action action, ICompositionRoot? services = null) => Run(action.Method, services);

    public static void Run(MethodBase callback, ICompositionRoot? services = null)
    {
        var alc = CreateAssemblyLoadContext();
        var compositionRoot = services ?? (IServiceProvider)new DefaultCompositionRoot();
        
        Assembly? assembly = null;
        try
        {
            assembly = Patch(callback, compositionRoot, alc);
            Run(assembly, callback);
        }
        finally
        {
            // TODO: Could be async without await (timeout for that?)
            if (assembly != null) _compositionRoots.Remove(assembly);
            alc.Unload();
        }
    }

    private static AssemblyLoadContext CreateAssemblyLoadContext() => new("AutoFake", isCollectible: true);

    public static Assembly Patch(MethodBase callback) => Patch(callback, new DefaultCompositionRoot(), CreateAssemblyLoadContext());

    public static Assembly Patch(MethodBase callback, IServiceProvider services, AssemblyLoadContext alc)
    {
        var fakeCallback = services.Resolve<IFakeCallback>();
        fakeCallback.Patch(callback);
        LoadPatchedAssemblies(services, alc);
        var alcAsm = alc.LoadFromAssemblyPath(callback.Module.FullyQualifiedName); // TODO: is there a need to check if loaded?
        _compositionRoots.Add(alcAsm, services);
        return alcAsm;
    }

    private static void LoadPatchedAssemblies(IServiceProvider services, AssemblyLoadContext alc)
    {
        var assemblies = new Dictionary<ModuleDefinition, Assembly>();
        foreach (var patch in services.Resolve<IPatchCollection>())
        {
            if (!assemblies.TryGetValue(patch.Module, out var assembly))
            {
                using var asm = new MemoryStream();
                using var symbols = new MemoryStream(); //TODO: create if needed
                var writerParameters = new WriterParameters();
                if (Debugger.IsAttached)
                {
                    patch.Module.ReadSymbols();
                    writerParameters.SymbolStream = symbols;
                    writerParameters.SymbolWriterProvider = new SymbolsWriterProvider();
                }

                patch.Module.Write(asm, writerParameters);
                asm.Position = symbols.Position = 0;
                assembly = alc.LoadFromStream(asm, symbols);
                assemblies.Add(patch.Module, assembly);
            }
            
            patch.LoadType(assembly);
        }
    }

    public static IServiceProvider GetServices() => GetCompositionRoot(Assembly.GetCallingAssembly());

    public static IPatchConfiguration Patch<TInput, TReturn>(Func<TInput, TReturn> entryPoint)
    {
        return GetPatchConfiguration(entryPoint.Method);
    }

    public static IPatchConfiguration Patch<TInput>(Action<TInput> entryPoint)
    {
        return GetPatchConfiguration(entryPoint.Method);
    }
    
    public static IPatchConfiguration Patch<TReturn>(Func<TReturn> entryPoint)
    {
        return GetPatchConfiguration(entryPoint.Method);
    }
    
    private static IPatchConfiguration GetPatchConfiguration(MethodBase entryPoint)
    {
        var services = GetCompositionRoot(entryPoint);
        var factory = services.Resolve<Func<MethodBase,IPatchConfiguration>>();
        return factory(entryPoint);
    }
    
    private static IServiceProvider GetCompositionRoot(MethodBase entryPoint) => GetCompositionRoot(entryPoint.Module.Assembly);

    private static IServiceProvider GetCompositionRoot(Assembly assembly) => _compositionRoots[assembly];

    private static void Run(Assembly assembly, MethodBase callback)
    {
        var type = callback.DeclaringType ?? throw new ArgumentNullException("callback.DeclaringType");
        
        var alcType = assembly.GetType(
                          type?.FullName ?? throw new InvalidOperationException("Cannot find the action type"))
                      ?? throw new MissingMemberException($"Cannot find {type.FullName}");
        var instance = Activator.CreateInstance(alcType);
        var alcMethod = alcType.GetMethod(callback.Name, BindingFlags.Instance | BindingFlags.NonPublic)
                        ?? throw new MissingMethodException(type.FullName, callback.Name);
        // TODO: Could be async requiring await
        alcMethod.Invoke(instance, null);
    }

    public static bool ValidateArguments(string patchKey, object[] arguments)
    {
        var services = GetCompositionRoot(Assembly.GetCallingAssembly());
        var patchCollection = services.Resolve<IPatchCollection>();
        var patch = patchCollection.GetPatch(patchKey);
        return patch.Arguments.SequenceEqual(arguments);
    }
    
    private class SymbolsWriterProvider : ISymbolWriterProvider
    {
        public ISymbolWriter GetSymbolWriter(ModuleDefinition module, string fileName) => throw new NotSupportedException("Symbols should be added without files");

        public ISymbolWriter? GetSymbolWriter(ModuleDefinition module, Stream symbolStream)
        {
            return module.HasSymbols ? module.SymbolReader.GetWriterProvider().GetSymbolWriter(module, symbolStream) : null;
        }
    }
}