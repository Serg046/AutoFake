using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using System.Linq;
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

        public void RemoveMethodArgumentsIfAny()
        {
            if (_instruction.Operand is MethodReference method)
            {
                for (var i = 0; i < method.Parameters.Count; i++)
                {
                    RemoveStackArgument();
                }
            }
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
                    _typeInfo.Module.ImportReference(typeof(Action).GetMethod(nameof(Action.Invoke)))));
            }
            else
            {
                _emitter.InsertAfter(_instruction, Instruction.Create(OpCodes.Call,
                    _typeInfo.Module.ImportReference(typeof(Action).GetMethod(nameof(Action.Invoke)))));
                _emitter.InsertAfter(_instruction, Instruction.Create(OpCodes.Ldsfld, closure));
            }
        }

        public IList<VariableDefinition> SaveMethodCall(FieldDefinition accumulator, bool checkArguments)
        {
            var method = (MethodReference)_instruction.Operand;
            var variables = new Stack<VariableDefinition>();
            foreach (var parameter in method.Parameters.Reverse())
            {
                var variable = new VariableDefinition(parameter.ParameterType);
                variables.Push(variable);
                _emitter.Body.Variables.Add(variable);
                _emitter.InsertBefore(_instruction, Instruction.Create(OpCodes.Stloc, variable));
            }
            var objRef = _typeInfo.Module.ImportReference(typeof(object));
            _emitter.InsertBefore(_instruction, Instruction.Create(OpCodes.Ldc_I4, variables.Count));
            _emitter.InsertBefore(_instruction, Instruction.Create(OpCodes.Newarr, objRef));
            var arrVar = new VariableDefinition(_typeInfo.Module.ImportReference(typeof(object[])));
            _emitter.Body.Variables.Add(arrVar);
            _emitter.InsertBefore(_instruction, Instruction.Create(OpCodes.Stloc, arrVar));

            SaveMethodArguments(checkArguments, variables, arrVar);

            _emitter.InsertBefore(_instruction, Instruction.Create(OpCodes.Ldsfld, accumulator));
            _emitter.InsertBefore(_instruction, Instruction.Create(OpCodes.Ldloc, arrVar));
            var addMethod = _typeInfo.Module.ImportReference(typeof(List<object[]>).GetMethod(nameof(List<object[]>.Add)));
            _emitter.InsertBefore(_instruction, Instruction.Create(OpCodes.Call, addMethod));

            return variables.ToList();
        }

        private void SaveMethodArguments(bool checkArguments, IEnumerable<VariableDefinition> variables, VariableDefinition array)
        {
            if (!checkArguments) return;

            var counter = 0;
            foreach (var variable in variables)
            {
                _emitter.InsertBefore(_instruction, Instruction.Create(OpCodes.Ldloc, array));
                _emitter.InsertBefore(_instruction, Instruction.Create(OpCodes.Ldc_I4, counter++));
                _emitter.InsertBefore(_instruction, Instruction.Create(OpCodes.Ldloc, variable));
                if (variable.VariableType.IsValueType)
                {
                    _emitter.InsertBefore(_instruction, Instruction.Create(OpCodes.Box, variable.VariableType));
                }
                _emitter.InsertBefore(_instruction, Instruction.Create(OpCodes.Stelem_Ref));
            }
        }
    }
}
