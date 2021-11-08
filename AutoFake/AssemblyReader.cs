using System;
using System.Diagnostics;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake
{
	internal class AssemblyReader : IAssemblyReader
	{
		private readonly Lazy<TypeDefinition> _sourceTypeDefinition;
		private readonly Lazy<TypeDefinition> _fieldsTypeDefinition;

		public AssemblyReader(Type sourceType, FakeOptions fakeOptions)
		{
			SourceType = sourceType;
			_sourceTypeDefinition = new Lazy<TypeDefinition>(() => GetSourceTypeDefinition(sourceType, fakeOptions));
			_fieldsTypeDefinition = new Lazy<TypeDefinition>(() => GetFieldsTypeDef(SourceTypeDefinition, fakeOptions));
		}

		public Type SourceType { get; }

		public TypeDefinition SourceTypeDefinition => _sourceTypeDefinition.Value;

		public TypeDefinition FieldsTypeDefinition => _fieldsTypeDefinition.Value;

		private static TypeDefinition GetSourceTypeDefinition(Type sourceType, FakeOptions fakeOptions)
		{
			var readerParameters = new ReaderParameters
			{
				ReadSymbols = fakeOptions.Debug == DebugMode.Enabled || (fakeOptions.Debug == DebugMode.Auto && Debugger.IsAttached),
				SymbolReaderProvider = new DefaultSymbolReaderProvider(throwIfNoSymbol: false)
			};
			var assemblyDef = AssemblyDefinition.ReadAssembly(sourceType.Module.FullyQualifiedName, readerParameters);
			assemblyDef.Name.Name += "Fake";
			assemblyDef.MainModule.ImportReference(sourceType);
			return assemblyDef.MainModule.GetType(sourceType.FullName, runtimeName: true).ToTypeDefinition();
		}

		private static TypeDefinition GetFieldsTypeDef(TypeDefinition sourceTypeDef, FakeOptions options)
		{
			var module = sourceTypeDef.Module;
			if (options.IsMultipleAssembliesMode)
			{
				var name = $"AutoFakeFields{Guid.NewGuid()}";
				module = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition(name, new Version(1, 0)), name, ModuleKind.Dll).MainModule;
			}
			var typeDef = new TypeDefinition("AutoFake", "Fields", TypeAttributes.Class | TypeAttributes.Public, module.TypeSystem.Object);
			module.Types.Add(typeDef);
			return typeDef;
		}
	}
}
