using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake
{
    internal interface IProcessor
    {
        void InjectClosure(ClosureDescriptor closure, bool beforeInstruction);
        void PushMethodArguments(IEnumerable<VariableDefinition> variables);
        void RemoveInstruction(Instruction instruction);
        void RemoveMethodArgumentsIfAny();
        void RemoveStackArgument();
        void ReplaceToRetValueField(FieldDefinition retField);
        IList<VariableDefinition> SaveMethodCall(FieldDefinition accumulator, bool checkArguments);
    }
}