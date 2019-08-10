using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake
{
    internal interface IProcessor
    {
        void InjectCallback(MethodDescriptor callback, bool beforeInstruction);
        void PushMethodArguments(IEnumerable<VariableDefinition> variables);
        void RemoveInstruction(Instruction instruction);
        void RemoveMethodArgumentsIfAny();
        void RemoveStackArgument();
        void ReplaceToRetValueField(FieldDefinition retField);
        IList<VariableDefinition> SaveMethodCall(VariableDefinition accumulator, bool checkArguments);
    }
}