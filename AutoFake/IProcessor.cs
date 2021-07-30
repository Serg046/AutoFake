using System;
using System.Collections.Generic;
using AutoFake.Setup.Mocks;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake
{
    internal interface IProcessor
    {
        void InjectClosure(FieldDefinition closure, InsertMock.Location location);
        void PushMethodArguments(IEnumerable<VariableDefinition> variables);
        void RemoveStackArgument();
        IList<VariableDefinition> SaveMethodCall(FieldDefinition setupBody, FieldDefinition executionContext, IList<Type> argumentTypes);
    }
}