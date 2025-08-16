using System.Reflection;
using AutoFake.Abstractions.Setup.Patches;
using Mono.Cecil;

namespace AutoFake.Abstractions.Setup;

public interface IPatchCollection : IEnumerable<IPatch>
{
    void AddPatch(IPatch patch);
    IPatch GetPatch(MethodBase patchCallback);
    IPatch GetPatch(string key);
}