using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake
{
    internal interface IProcessor
    {
        void PushMethodArguments(IEnumerable<VariableDefinition> variables);
        void RemoveStackArgument();
        IReadOnlyList<VariableDefinition> RecordMethodCall(FieldDefinition setupBody, FieldDefinition executionContext, IReadOnlyList<Type> argumentTypes);
    }
}