using System.Reflection;
using System.Runtime.Loader;
using AutoFake.Setup.Patches;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MethodBody = Mono.Cecil.Cil.MethodBody;

namespace AutoFake.Abstractions.Setup.Patches;

public interface IPatch
{
    string Key { get; }
    ModuleDefinition Module { get; }
    FieldDefinition? RetValueField { get; }
    object[] Arguments { get; }
    IPatchMember PatchMember { get; }
    bool IsMatch(Instruction instruction);
    void Inject(MethodBody method, Instruction instruction);
    Type? PatchedType { get; }
    void LoadType(Assembly assembly);
}