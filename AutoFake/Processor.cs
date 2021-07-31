using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using System.Linq;
using AutoFake.Expression;

namespace AutoFake
{
    internal class Processor : IProcessor
    {
        private readonly IEmitter _emitter;
        private readonly Instruction _instruction;

        public Processor(IEmitter emitter, Instruction instruction)
        {
            _emitter = emitter;
            _instruction = instruction;
        }

        public void RemoveStackArgument() => _emitter.InsertBefore(_instruction, Instruction.Create(OpCodes.Pop));

        public void PushMethodArguments(IEnumerable<VariableDefinition> variables)
        {
            foreach (var variable in variables)
            {
                _emitter.InsertBefore(_instruction, Instruction.Create(OpCodes.Ldloc, variable));
            }
        }

        public IList<VariableDefinition> RecordMethodCall(FieldDefinition setupBody, FieldDefinition executionContext, IList<Type> argumentTypes)
        {
	        var module = ((MemberReference)_instruction.Operand).Module;
	        var variables = new List<VariableDefinition>();
	        foreach (var argType in argumentTypes)
	        {
		        var variable = new VariableDefinition(module.ImportReference(argType));
		        variables.Add(variable);
		        _emitter.Body.Variables.Add(variable);
	        }

	        foreach (var variable in variables.Select(v => v).Reverse())
	        {
		        _emitter.InsertBefore(_instruction, Instruction.Create(OpCodes.Stloc, variable));
	        }

            var objRef = module.ImportReference(typeof(object));
            _emitter.InsertBefore(_instruction, Instruction.Create(OpCodes.Ldc_I4, variables.Count));
            _emitter.InsertBefore(_instruction, Instruction.Create(OpCodes.Newarr, objRef));
            var arrVar = new VariableDefinition(module.ImportReference(typeof(object[])));
            _emitter.Body.Variables.Add(arrVar);
            _emitter.InsertBefore(_instruction, Instruction.Create(OpCodes.Stloc, arrVar));

            RecordMethodCall(variables, arrVar, argumentTypes);

            var verifyMethodInfo = typeof(InvocationExpression).GetMethod(nameof(InvocationExpression.VerifyArguments));
            var verifyMethodRef = module.ImportReference(verifyMethodInfo);
            _emitter.InsertBefore(_instruction, Instruction.Create(OpCodes.Ldsfld, module.ImportReference(setupBody)));
            _emitter.InsertBefore(_instruction, Instruction.Create(OpCodes.Ldloc, arrVar));
            _emitter.InsertBefore(_instruction, Instruction.Create(OpCodes.Call, verifyMethodRef));

            var incMethodInfo = typeof(ExecutionContext).GetMethod(nameof(ExecutionContext.IncActualCalls));
            var incMethodRef = module.ImportReference(incMethodInfo);
            _emitter.InsertBefore(_instruction, Instruction.Create(OpCodes.Ldsfld, module.ImportReference(executionContext)));
            _emitter.InsertBefore(_instruction, Instruction.Create(OpCodes.Call, incMethodRef));

            return variables;
        }

        private void RecordMethodCall(IList<VariableDefinition> variables, VariableDefinition array, IList<Type> argumentTypes)
        {
	        for (var i = 0; i < variables.Count; i++)
	        {
		        var variable = variables[i];
		        _emitter.InsertBefore(_instruction, Instruction.Create(OpCodes.Ldloc, array));
		        _emitter.InsertBefore(_instruction, Instruction.Create(OpCodes.Ldc_I4, i));
		        _emitter.InsertBefore(_instruction, Instruction.Create(OpCodes.Ldloc, variable));
		        if (argumentTypes[i].IsValueType)
		        {
			        _emitter.InsertBefore(_instruction, Instruction.Create(OpCodes.Box, variable.VariableType));
		        }

		        _emitter.InsertBefore(_instruction, Instruction.Create(OpCodes.Stelem_Ref));
	        }
        }
    }
}
