using System;
using Mono.Cecil;

namespace AutoFake.Abstractions
{
	internal interface IAssemblyReader
	{
		Type SourceType { get; }
		TypeDefinition SourceTypeDefinition { get; }
		TypeDefinition FieldsTypeDefinition { get; }
	}
}