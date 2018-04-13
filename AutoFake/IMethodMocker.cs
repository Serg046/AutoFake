using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake
{
    internal interface IMethodMocker
    {
        ITypeInfo TypeInfo { get; }
        MockedMemberInfo MemberInfo { get; }
        void InjectCurrentPositionSaving(ILProcessor ilProcessor, Instruction instruction);
        IList<FieldDefinition> PopMethodArguments(ILProcessor ilProcessor, Instruction instruction);
        void RemoveMethodArguments(ILProcessor ilProcessor, Instruction instruction);
        void RemoveStackArgument(ILProcessor ilProcessor, Instruction instruction);
        void PushMethodArguments(ILProcessor ilProcessor, Instruction instruction, IEnumerable<FieldDefinition> arguments);
        void RemoveInstruction(ILProcessor ilProcessor, Instruction instruction);
        void ReplaceToRetValueField(ILProcessor ilProcessor, Instruction instruction);
        void InjectCallback(ILProcessor ilProcessor, Instruction instruction);
    }
}