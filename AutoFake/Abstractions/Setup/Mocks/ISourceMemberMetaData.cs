using AutoFake.Abstractions.Expression;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace AutoFake.Abstractions.Setup.Mocks;

internal interface ISourceMemberMetaData
{
	IExecutionContext.CallsCheckerFunc? ExpectedCalls { get; set; }
	IInvocationExpression InvocationExpression { get; }
	IPrePostProcessor PrePostProcessor { get; }
	ISourceMember SourceMember { get; }
	Func<bool>? WhenFunc { get; set; }

	void AfterInjection(IEmitter emitter);
	void BeforeInjection(MethodDefinition method);
	FieldInfo? GetField(Type type, string fieldName);
	string GetFieldName(string prefix, string suffix);
	void Initialize(Type? type);
	IReadOnlyList<VariableDefinition> RecordMethodCall(IProcessor processor);
}
