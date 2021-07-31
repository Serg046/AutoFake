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
        IList<VariableDefinition> RecordMethodCall(FieldDefinition setupBody, FieldDefinition executionContext, IList<Type> argumentTypes);
    }
}