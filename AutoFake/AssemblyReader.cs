using System;
using AutoFake.Abstractions;
using Mono.Cecil;

namespace AutoFake;

internal class AssemblyReader : IAssemblyReader
{
	private readonly ICecilFactory _cecilFactory;
	private readonly Lazy<TypeDefinition> _sourceTypeDefinition;
	private readonly Lazy<TypeDefinition> _fieldsTypeDefinition;

	public AssemblyReader(Type sourceType, IOptions options, ICecilFactory cecilFactory)
	{
		_cecilFactory = cecilFactory;
		SourceType = sourceType;
		_sourceTypeDefinition = new Lazy<TypeDefinition>(() => GetSourceTypeDefinition(sourceType, options));
		_fieldsTypeDefinition = new Lazy<TypeDefinition>(() => GetFieldsTypeDef(SourceTypeDefinition, options));
	}

	public Type SourceType { get; }

	public TypeDefinition SourceTypeDefinition => _sourceTypeDefinition.Value;

	public TypeDefinition FieldsTypeDefinition => _fieldsTypeDefinition.Value;

	private TypeDefinition GetSourceTypeDefinition(Type sourceType, IOptions options)
	{
		var readerParameters = _cecilFactory.CreateReaderParameters();
		readerParameters.ReadSymbols = options.IsDebugEnabled;
		if (readerParameters.ReadSymbols)
		{
			readerParameters.SymbolReaderProvider = _cecilFactory.CreateSymbolReaderProvider(throwIfNoSymbol: false);
		}
		var assemblyDef = AssemblyDefinition.ReadAssembly(sourceType.Module.FullyQualifiedName, readerParameters);
		if (options.Debug == DebugMode.Enabled && !assemblyDef.MainModule.HasSymbols) throw new InvalidOperationException("No symbols found");
		assemblyDef.MainModule.ImportReference(sourceType);
		return assemblyDef.MainModule.GetType(sourceType.FullName, runtimeName: true).ToTypeDefinition();
	}

	private TypeDefinition GetFieldsTypeDef(TypeDefinition sourceTypeDef, IOptions options)
	{
		if (!options.IsMultipleAssembliesMode) return sourceTypeDef;

		var name = $"AutoFakeFields{Guid.NewGuid()}";
		var module = AssemblyDefinition.CreateAssembly(_cecilFactory.CreateAssemblyNameDefinition(name, new Version(1, 0)), name, ModuleKind.Dll).MainModule;
		var typeDef = _cecilFactory.CreateTypeDefinition("AutoFake", "Fields", TypeAttributes.Class | TypeAttributes.Public, module.TypeSystem.Object);
		module.Types.Add(typeDef);
		return typeDef;
	}
}
