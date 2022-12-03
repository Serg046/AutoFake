using System;
using Mono.Cecil;

namespace AutoFake.Abstractions;

public interface IPrePostProcessor
{
	FieldDefinition GenerateField(string name, Type returnType);
	void InjectVerification(IEmitter emitter, FieldDefinition setupBody, FieldDefinition executionContext);
}
