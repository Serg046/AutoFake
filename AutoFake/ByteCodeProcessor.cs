using AutoFake.Abstractions;
using AutoFake.Abstractions.Setup;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake;

internal class ByteCodeProcessor(IEmitter emitter) : IByteCodeProcessor
{
    public IReadOnlyList<VariableDefinition> RecordMethodCall(FieldDefinition argumentsField, IPatchMember patchMember)
    {
        var variables = PushArgumentsToVariables(patchMember);
        var arrVar = GetArgumentsArray(argumentsField.Module, variables);
        RecordMethodCall(variables, arrVar, patchMember);
        return variables;
    }
    
    private IReadOnlyList<VariableDefinition> PushArgumentsToVariables(IPatchMember patchMember)
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
            emitter.InsertAbove(Instruction.Create(OpCodes.Stloc, variable));
        }

        return variables;
    }
    
    private VariableDefinition GetArgumentsArray(ModuleDefinition module, IReadOnlyList<VariableDefinition> variables)
    {
        emitter.InsertAbove(Instruction.Create(OpCodes.Ldc_I4, variables.Count));
        emitter.InsertAbove(Instruction.Create(OpCodes.Newarr, module.TypeSystem.Object));
        var arrVar = new VariableDefinition(module.ImportReference(typeof(object[])));
        emitter.Method.Variables.Add(arrVar);
        emitter.InsertAbove(Instruction.Create(OpCodes.Stloc, arrVar));
        return arrVar;
    }
    
    private void RecordMethodCall(IReadOnlyList<VariableDefinition> variables, VariableDefinition array, IPatchMember patchMember)
    {
        var parameters = patchMember.GetParameters();
        for (var i = 0; i < variables.Count; i++)
        {
            var variable = variables[i];
            emitter.InsertAbove(Instruction.Create(OpCodes.Ldloc, array));
            emitter.InsertAbove(Instruction.Create(OpCodes.Ldc_I4, i));
            emitter.InsertAbove(Instruction.Create(OpCodes.Ldloc, variable));
            if (parameters[i].ParameterType.IsValueType)
            {
                emitter.InsertAbove(Instruction.Create(OpCodes.Box, variable.VariableType));
            }

            emitter.InsertAbove(Instruction.Create(OpCodes.Stelem_Ref));
        }
    }
}