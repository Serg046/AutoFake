using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Abstractions.Setup;

public interface IPatchMember
{
    string Name { get; }
    bool HasThis { get; }
    TypeReference ReturnType { get; }
    IReadOnlyList<ParameterDefinition> GetParameters();
    bool IsMatch(Instruction instruction);
}