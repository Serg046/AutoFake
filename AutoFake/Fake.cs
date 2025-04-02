using System.Reflection;
using System.Runtime.Loader;
using AutoFake.Abstractions;
using AutoFake.Abstractions.Setup;
using AutoFake.Abstractions.Setup.Configurations;
using IServiceProvider = AutoFake.Abstractions.IServiceProvider;

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
        
        foreach (var patch in services.Resolve<IPatchCollection>())
        {
            patch.LoadAssembly(alc);
        }
        
        var alcAsm = alc.LoadFromAssemblyPath(callback.Module.FullyQualifiedName); // TODO: is there a need to check if loaded?
        _compositionRoots.Add(alcAsm, services);
        return alcAsm;
    }

    public static IServiceProvider GetServices() => GetCompositionRoot(Assembly.GetCallingAssembly());

    public static IPatchConfiguration Patch<TInput, TReturn>(Func<TInput, TReturn> entryPoint)
    {
        return GetCompositionRoot(entryPoint.Method).Resolve<IPatchConfiguration>();
    }

    public static IPatchConfiguration Patch<TInput>(Action<TInput> entryPoint)
    {
        return GetCompositionRoot(entryPoint.Method).Resolve<IPatchConfiguration>();
    }
    
    public static IPatchConfiguration Patch<TReturn>(Func<TReturn> entryPoint)
    {
        return GetCompositionRoot(entryPoint.Method).Resolve<IPatchConfiguration>();
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

    public static bool ValidateArguments(object[] arguments)
    {
        return true;
    }
}