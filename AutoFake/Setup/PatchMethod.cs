using AutoFake.Abstractions.Setup;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup;

internal class PatchMethod(MethodReference patch) : IPatchMember
{
    public string Name => patch.Name;
    public bool HasThis => patch.HasThis;
    public TypeReference ReturnType => patch.ReturnType;
    public IReadOnlyList<ParameterDefinition> GetParameters() => patch.Parameters.ToArray();

    public bool IsMatch(Instruction instruction)
    {
        return instruction.OpCode.Code is Code.Call or Code.Callvirt
               && instruction.Operand is MethodReference methodRef
               && methodRef == patch;
    }
}