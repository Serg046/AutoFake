using System;
using System.Collections.Generic;
using System.Reflection;
using Mono.Cecil;

namespace AutoFake.Abstractions;

public interface ITypeInfo
{
	bool IsMultipleAssembliesMode { get; }
	Type SourceType { get; }
	ITypeMap TypeMap { get; }
	bool IsInFakeModule(TypeReference type);
	IEnumerable<MethodDefinition> GetConstructors();
	MethodDefinition? GetMethod(MethodBase method, bool searchInBaseType = false);
	MethodDefinition? GetMethod(TypeDefinition type, MethodBase method, bool searchInBaseType = false);
	TypeDefinition GetTypeDefinition(Type type);
	ICollection<MethodDefinition> GetAllImplementations(MethodDefinition method, bool includeAffectedAssemblies = false);
	bool IsInReferencedAssembly(AssemblyDefinition assembly);
	TypeReference ImportToSourceAsm(Type type);
	FieldReference ImportToSourceAsm(FieldInfo field);
	MethodReference ImportToSourceAsm(MethodBase method);
	TypeReference ImportToSourceAsm(TypeReference type);
	MethodReference ImportToSourceAsm(MethodReference method);
	TypeReference ImportToFieldsAsm(Type type);
}
