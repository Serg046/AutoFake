using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using System.Linq;

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

        public void InjectClosure(ClosureDescriptor closure, bool beforeInstruction,
            IDictionary<CapturedMember, FieldDefinition> generatedMembers)
        {
            var instructions = GetInstructions(closure, generatedMembers);
            if (!beforeInstruction) instructions = instructions.Reverse();
            var emit = beforeInstruction ? _emitter.InsertBefore : (Action<Instruction, Instruction>)_emitter.InsertAfter;
            foreach (var instruction in instructions)
            {
                emit(_instruction, instruction);
            }
        }

        private IEnumerable<Instruction> GetInstructions(ClosureDescriptor closure,
            IDictionary<CapturedMember, FieldDefinition> generatedMembers)
        {
            var type = _typeInfo.Module.GetType(closure.DeclaringType, true).Resolve();
            var ctor = type.Methods.Single(m => m.Name == ".ctor");
            var method = type.Methods.Single(m => m.Name == closure.Name);
            yield return Instruction.Create(OpCodes.Newobj, ctor);

            foreach (var member in generatedMembers)
            {
                yield return Instruction.Create(OpCodes.Dup);
                yield return Instruction.Create(OpCodes.Ldsfld, member.Value);
                yield return Instruction.Create(OpCodes.Stfld, member.Key.Field);

            }

            yield return Instruction.Create(OpCodes.Call, method);
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
