using System;
using Mono.Cecil;

namespace AutoFake.Abstractions;

public interface IAssemblyReader
{
	Type SourceType { get; }
	TypeDefinition SourceTypeDefinition { get; }
	TypeDefinition FieldsTypeDefinition { get; }
}
