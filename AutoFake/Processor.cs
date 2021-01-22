using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using System.Linq;
using AutoFake.Expression;
using AutoFake.Setup.Mocks;

namespace AutoFake
{
    internal class Processor : IProcessor
    {
        private readonly ITypeInfo _typeInfo;
        private readonly IEmitter _emitter;
        private readonly Instruction _instruction;

        public Processor(ITypeInfo typeInfo, IEmitter emitter, Instruction instruction)
        {
            _typeInfo = typeInfo;
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

        public void ReplaceToRetValueField(FieldDefinition retField)
            => _emitter.Replace(_instruction, Instruction.Create(OpCodes.Ldsfld, retField));

        public void RemoveInstruction(Instruction instruction) => _emitter.Remove(instruction);

        public void InjectClosure(FieldDefinition closure, InsertMock.Location location)
        {
            if (location == InsertMock.Location.Top)
            {
                _emitter.InsertBefore(_instruction, Instruction.Create(OpCodes.Ldsfld, closure));
                _emitter.InsertBefore(_instruction, Instruction.Create(OpCodes.Call,
                    _typeInfo.ImportReference(typeof(Action).GetMethod(nameof(Action.Invoke)))));
            }
            else
            {
                _emitter.InsertAfter(_instruction, Instruction.Create(OpCodes.Call,
                    _typeInfo.ImportReference(typeof(Action).GetMethod(nameof(Action.Invoke)))));
                _emitter.InsertAfter(_instruction, Instruction.Create(OpCodes.Ldsfld, closure));
            }
        }

        public IList<VariableDefinition> SaveMethodCall(FieldDefinition setupBody, FieldDefinition executionContext, IEnumerable<Type> argumentTypes)
        {
            var variables = new List<VariableDefinition>();
            if (_instruction.Operand is MethodReference method)
            {
	            foreach (var parameter in method.Parameters)
	            {
		            var variable = new VariableDefinition(parameter.ParameterType);
		            variables.Add(variable);
		            _emitter.Body.Variables.Add(variable);
	            }

	            foreach (var variable in variables.Select(v => v).Reverse())
	            {
		            _emitter.InsertBefore(_instruction, Instruction.Create(OpCodes.Stloc, variable));
	            }
            }

            var objRef = _typeInfo.ImportReference(typeof(object));
			_emitter.InsertBefore(_instruction, Instruction.Create(OpCodes.Ldc_I4, variables.Count));
			_emitter.InsertBefore(_instruction, Instruction.Create(OpCodes.Newarr, objRef));
			var arrVar = new VariableDefinition(_typeInfo.ImportReference(typeof(object[])));
			_emitter.Body.Variables.Add(arrVar);
			_emitter.InsertBefore(_instruction, Instruction.Create(OpCodes.Stloc, arrVar));

			SaveMethodArguments(variables, arrVar, argumentTypes);

			var verifyMethodInfo = typeof(InvocationExpression).GetMethod(nameof(InvocationExpression.VerifyArguments));
			var verifyMethodRef = _typeInfo.ImportReference(verifyMethodInfo);
			_emitter.InsertBefore(_instruction, Instruction.Create(OpCodes.Ldsfld, setupBody));
			_emitter.InsertBefore(_instruction, Instruction.Create(OpCodes.Ldloc, arrVar));
			_emitter.InsertBefore(_instruction, Instruction.Create(OpCodes.Call, verifyMethodRef));

			var incMethodInfo = typeof(ExecutionContext).GetMethod(nameof(ExecutionContext.IncActualCalls));
			var incMethodRef = _typeInfo.ImportReference(incMethodInfo);
			_emitter.InsertBefore(_instruction, Instruction.Create(OpCodes.Ldsfld, executionContext));
			_emitter.InsertBefore(_instruction, Instruction.Create(OpCodes.Call, incMethodRef));

			return variables;
        }

        private void SaveMethodArguments(IEnumerable<VariableDefinition> variables, VariableDefinition array, IEnumerable<Type> argumentTypes)
        {
	        var counter = 0;
            foreach (var item in variables.Zip(argumentTypes, (var, type) => new {Var = var, Type = type}))
            {
                _emitter.InsertBefore(_instruction, Instruction.Create(OpCodes.Ldloc, array));
                _emitter.InsertBefore(_instruction, Instruction.Create(OpCodes.Ldc_I4, counter++));
                _emitter.InsertBefore(_instruction, Instruction.Create(OpCodes.Ldloc, item.Var));
                if (item.Type.IsValueType)
                {
                    _emitter.InsertBefore(_instruction, Instruction.Create(OpCodes.Box, item.Var.VariableType));
                }
                _emitter.InsertBefore(_instruction, Instruction.Create(OpCodes.Stelem_Ref));
            }
        }
    }
}
