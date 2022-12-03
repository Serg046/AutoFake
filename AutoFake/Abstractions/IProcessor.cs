using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Abstractions;

public interface IProcessor
{
	void PushMethodArguments(IEnumerable<VariableDefinition> variables);
	void RemoveStackArgument();
	IReadOnlyList<VariableDefinition> RecordMethodCall(FieldDefinition setupBody, FieldDefinition executionContext, IReadOnlyList<Type> argumentTypes);
}
