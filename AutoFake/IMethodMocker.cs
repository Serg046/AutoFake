using System.Collections.Generic;
using Mono.Cecil.Cil;

namespace AutoFake
{
    internal interface IMethodMocker
    {
        ITypeInfo TypeInfo { get; }
        MockedMemberInfo MemberInfo { get; }
        void RemoveMethodArgumentsIfAny(ILProcessor ilProcessor, Instruction instruction);
        void RemoveStackArgument(ILProcessor ilProcessor, Instruction instruction);
        void PushMethodArguments(ILProcessor ilProcessor, Instruction instruction, IEnumerable<VariableDefinition> variables);
        void RemoveInstruction(ILProcessor ilProcessor, Instruction instruction);
        void ReplaceToRetValueField(ILProcessor ilProcessor, Instruction instruction);
        void InjectCallback(ILProcessor ilProcessor, Instruction instruction);
        void InjectVerification(ILProcessor ilProcessor);
        IList<VariableDefinition> SaveMethodCall(ILProcessor ilProcessor, Instruction instruction);
    }
}