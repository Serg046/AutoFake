using AutoFake.Abstractions.Setup;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Abstractions;

public interface IByteCodeProcessor
{
    IReadOnlyList<VariableDefinition> ReadMethodArguments(IPatchMember patchMember, VariableDefinition array, bool noBoxing = false);
    VariableDefinition CreateArrayVariable(ModuleDefinition module, int capacity);
}