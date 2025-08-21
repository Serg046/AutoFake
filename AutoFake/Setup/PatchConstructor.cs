using AutoFake.Abstractions.Setup;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup;

public class PatchConstructor(MethodReference patch) : IPatchMember
{
    public string Name => patch.Name;
    public bool HasThis => false;
    public TypeReference ReturnType => patch.DeclaringType;
    public IReadOnlyList<ParameterDefinition> GetParameters() => patch.Parameters.ToArray();

    public bool IsMatch(Instruction instruction)
    {
        return instruction.OpCode.Code is Code.Newobj
               && instruction.Operand is MethodReference methodRef
               && methodRef.Compare(patch);
    }
}