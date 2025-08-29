using AutoFake.Abstractions.Setup;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup;

internal class PatchMethod(MethodReference patch) : PatchMember(patch), IPatchMember
{
    public string Name => patch.Name;
    public bool HasThis => patch.HasThis;
    public TypeReference ReturnType => TryGetGenericReturnType(patch.ReturnType, out var returnType) ? returnType : patch.ReturnType;

    public IReadOnlyList<ParameterDefinition> GetParameters() => patch.Parameters.ToArray();

    public bool IsMatch(Instruction instruction)
    {
        return instruction.OpCode.Code is Code.Call or Code.Callvirt // TODO: Code.Calli?
               && instruction.Operand is MethodReference methodRef
               && IsMatch(methodRef);
    }
}