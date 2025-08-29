using AutoFake.Abstractions.Setup;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup;

internal class PatchField(FieldReference patch, bool hasThis) : PatchMember(patch), IPatchMember
{
    public const Code StaticFieldCode = Code.Ldsfld;
    public string Name => patch.Name;
    public bool HasThis => hasThis;
    public TypeReference ReturnType => TryGetGenericReturnType(patch.FieldType, out var returnType) ? returnType : patch.FieldType;
    public IReadOnlyList<ParameterDefinition> GetParameters() => [];

    public bool IsMatch(Instruction instruction)
    {
        return instruction.OpCode.Code is Code.Ldfld or StaticFieldCode //TODO Code.Ldflda or Code.Ldftn?
               && instruction.Operand is FieldReference field
               && IsMatch(field);
    }
}