using AutoFake.Abstractions;
using AutoFake.Abstractions.Setup;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake;

internal class ByteCodeProcessor(IEmitter emitter) : IByteCodeProcessor
{
    public IReadOnlyList<VariableDefinition> RecordMethodCall(IPatchMember patchMember, VariableDefinition array)
    {
        var variables = MoveArgumentsToVariables(patchMember);
        RecordMethodCall(variables, array, patchMember);
        return variables;
    }
    
    private IReadOnlyList<VariableDefinition> MoveArgumentsToVariables(IPatchMember patchMember)
    {
        var variables = new List<VariableDefinition>();
        foreach (var prm in patchMember.GetParameters())
        {
            var variable = new VariableDefinition(prm.ParameterType);
            variables.Add(variable);
            emitter.Method.Variables.Add(variable);
        }

        foreach (var variable in variables.Select(v => v).Reverse())
        {
            emitter.Emit(Instruction.Create(OpCodes.Stloc, variable));
        }

        return variables;
    }
    
    public VariableDefinition CreateArrayVariable(ModuleDefinition module, int capacity)
    {
        emitter.Emit(Instruction.Create(OpCodes.Ldc_I4, capacity));
        emitter.Emit(Instruction.Create(OpCodes.Newarr, module.TypeSystem.Object));
        var array = new VariableDefinition(module.ImportReference(typeof(object[])));
        emitter.Method.Variables.Add(array);
        emitter.Emit(Instruction.Create(OpCodes.Stloc, array));
        return array;
    }
    
    private void RecordMethodCall(IReadOnlyList<VariableDefinition> variables, VariableDefinition array, IPatchMember patchMember)
    {
        var parameters = patchMember.GetParameters();
        for (var i = 0; i < variables.Count; i++)
        {
            var variable = variables[i];
            emitter.Emit(Instruction.Create(OpCodes.Ldloc, array));
            emitter.Emit(Instruction.Create(OpCodes.Ldc_I4, i));
            emitter.Emit(Instruction.Create(OpCodes.Ldloc, variable));
            if (parameters[i].ParameterType.IsValueType)
            {
                emitter.Emit(Instruction.Create(OpCodes.Box, variable.VariableType));
            }

            emitter.Emit(Instruction.Create(OpCodes.Stelem_Ref));
        }
    }
}