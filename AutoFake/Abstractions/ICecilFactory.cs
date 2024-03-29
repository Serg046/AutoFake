using System;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Abstractions;

public interface ICecilFactory
{
	VariableDefinition CreateVariable(TypeReference variableType);
	ReaderParameters CreateReaderParameters();
	WriterParameters CreateWriterParameters();
	ISymbolReaderProvider CreateSymbolReaderProvider(bool throwIfNoSymbol);
	AssemblyNameDefinition CreateAssemblyNameDefinition(string name, Version version);
	TypeDefinition CreateTypeDefinition(string @namespace, string name, TypeAttributes attributes, TypeReference baseType);
	MethodReference CreateMethodReference(string name, TypeReference returnType, TypeReference declaringType);
	ParameterDefinition CreateParameterDefinition(string name, ParameterAttributes attributes, TypeReference parameterType);
	GenericParameter CreateGenericParameter(string name, IGenericParameterProvider owner);
	FieldDefinition CreateFieldDefinition(string name, FieldAttributes attributes, TypeReference fieldType);
	GenericInstanceMethod CreateGenericInstanceMethod(MethodReference method);
	TypeReference CreateTypeReference(string @namespace, string name, ModuleDefinition module, IMetadataScope scope, bool valueType);
	GenericInstanceType CreateGenericInstanceType(TypeReference type);
}
