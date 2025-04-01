using System.Collections;
using System.Reflection;
using AutoFake.Abstractions.Setup;
using AutoFake.Abstractions.Setup.Patches;
using Mono.Cecil;

namespace AutoFake.Setup;

internal class PatchCollection : IPatchCollection
{
    private readonly Dictionary<string, IPatch> _patches = new();

    public void AddPatch(MethodReference patchCallback, IPatch patch)
    {
        var key = GetPatchKey(patchCallback.DeclaringType.GetClrTypeFullName(), patchCallback.Name);
        _patches.Add(key, patch);
    }

    public IPatch GetPatch(MethodBase patchCallback)
    {
        var typeName = patchCallback.DeclaringType?.FullName ?? throw new MissingMemberException("Cannot find a callback type");
        var key = GetPatchKey(typeName, patchCallback.Name);
        return _patches[key];
    }

    private string GetPatchKey(string type, string methodName) => $"{type}::{methodName}";
    
    public IEnumerator<IPatch> GetEnumerator() => _patches.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}