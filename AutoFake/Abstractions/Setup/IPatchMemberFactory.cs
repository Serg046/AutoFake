using System.Diagnostics.CodeAnalysis;

namespace AutoFake.Abstractions.Setup;

public interface IPatchMemberFactory
{
    bool TryCreatePatchMember(object operand, [NotNullWhen(true)] out IPatchMember? patchMember);
}