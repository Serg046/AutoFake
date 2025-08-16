using AutoFake.Abstractions.Setup;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup;

internal class PatchField(FieldReference patch) : IPatchMember
{
    public string Name => patch.Name;
    public bool HasThis => true; //TODO
    public TypeReference ReturnType => patch.FieldType;
    public IReadOnlyList<ParameterDefinition> GetParameters() => [];

    public bool IsMatch(Instruction instruction)
    {
        return instruction.OpCode.Code is Code.Ldfld or Code.Ldsfld //TODO Code.Ldflda or Code.Ldftn?
               && instruction.Operand is FieldReference field
               && field == patch;
    }
}