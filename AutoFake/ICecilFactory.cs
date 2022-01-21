using System;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake
{
	internal interface ICecilFactory
	{
		VariableDefinition CreateVariable(TypeReference variableType);
		ReaderParameters CreateReaderParameters();
		WriterParameters CreateWriterParameters();
		ISymbolReaderProvider CreateSymbolReaderProvider(bool throwIfNoSymbol);
		AssemblyNameDefinition CreateAssemblyNameDefinition(string name, Version version);
		TypeDefinition CreateTypeDefinition(string @namespace, string name, TypeAttributes attributes, TypeReference baseType);
	}
}