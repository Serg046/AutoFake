using AutoFake.Abstractions.Setup;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Abstractions;

public interface IByteCodeProcessor
{
    IReadOnlyList<VariableDefinition> RecordMethodCall(IPatchMember patchMember, VariableDefinition array);
    VariableDefinition CreateArrayVariable(ModuleDefinition module, int capacity);
}