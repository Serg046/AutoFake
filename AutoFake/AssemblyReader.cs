using System;
using System.Diagnostics;
using AutoFake.Abstractions;
using Mono.Cecil;

namespace AutoFake
{
	internal class AssemblyReader : IAssemblyReader
	{
		private readonly ICecilFactory _cecilFactory;
		private readonly Lazy<TypeDefinition> _sourceTypeDefinition;
		private readonly Lazy<TypeDefinition> _fieldsTypeDefinition;

		public AssemblyReader(Type sourceType, FakeOptions fakeOptions, ICecilFactory cecilFactory)
		{
			_cecilFactory = cecilFactory;
			SourceType = sourceType;
			_sourceTypeDefinition = new Lazy<TypeDefinition>(() => GetSourceTypeDefinition(sourceType, fakeOptions));
			_fieldsTypeDefinition = new Lazy<TypeDefinition>(() => GetFieldsTypeDef(SourceTypeDefinition, fakeOptions));
		}

		public Type SourceType { get; }

		public TypeDefinition SourceTypeDefinition => _sourceTypeDefinition.Value;

		public TypeDefinition FieldsTypeDefinition => _fieldsTypeDefinition.Value;

		private TypeDefinition GetSourceTypeDefinition(Type sourceType, FakeOptions fakeOptions)
		{
			var readerParameters = _cecilFactory.CreateReaderParameters();
			readerParameters.ReadSymbols = fakeOptions.Debug == DebugMode.Enabled || (fakeOptions.Debug == DebugMode.Auto && Debugger.IsAttached);
			readerParameters.SymbolReaderProvider = _cecilFactory.CreateSymbolReaderProvider(throwIfNoSymbol: false);
			var assemblyDef = AssemblyDefinition.ReadAssembly(sourceType.Module.FullyQualifiedName, readerParameters);
			assemblyDef.Name.Name += "Fake";
			assemblyDef.MainModule.ImportReference(sourceType);
			return assemblyDef.MainModule.GetType(sourceType.FullName, runtimeName: true).ToTypeDefinition();
		}

		private TypeDefinition GetFieldsTypeDef(TypeDefinition sourceTypeDef, FakeOptions options)
		{
			var module = sourceTypeDef.Module;
			if (options.IsMultipleAssembliesMode)
			{
				var name = $"AutoFakeFields{Guid.NewGuid()}";
				module = AssemblyDefinition.CreateAssembly(_cecilFactory.CreateAssemblyNameDefinition(name, new Version(1, 0)), name, ModuleKind.Dll).MainModule;
			}
			var typeDef = _cecilFactory.CreateTypeDefinition("AutoFake", "Fields", TypeAttributes.Class | TypeAttributes.Public, module.TypeSystem.Object);
			module.Types.Add(typeDef);
			return typeDef;
		}
	}
}
