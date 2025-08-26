using System.Diagnostics.CodeAnalysis;
using Mono.Cecil.Cil;

namespace AutoFake.Abstractions.Setup;

public interface IPatchMemberFactory
{
    bool TryCreatePatchMember(Instruction instruction, [NotNullWhen(true)] out IPatchMember? patchMember);
}