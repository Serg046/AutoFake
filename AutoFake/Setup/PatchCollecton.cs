using System.Collections;
using System.Reflection;
using AutoFake.Abstractions.Setup;
using AutoFake.Abstractions.Setup.Patches;
using Mono.Cecil;

namespace AutoFake.Setup;

internal class PatchCollection : IPatchCollection
{
    private readonly Dictionary<string, IPatch> _patches = new();

    public void AddPatch(IPatch patch)
    {
        _patches.Add(patch.Key, patch);
    }
    
    public static string GetPatchKey(MethodReference callback)
    {
        return GetPatchKey(callback.DeclaringType.GetClrTypeFullName(), callback.Name);
    }

    public IPatch GetPatch(MethodBase patchCallback)
    {
        var typeName = patchCallback.DeclaringType?.FullName ?? throw new MissingMemberException("Cannot find a callback type");
        var key = GetPatchKey(typeName, patchCallback.Name);
        return _patches[key];
    }
    
    public IPatch GetPatch(string key)
    {
        return _patches[key];
    }

    private static string GetPatchKey(string type, string methodName) => $"{type}::{methodName}";
    
    public IEnumerator<IPatch> GetEnumerator() => _patches.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}