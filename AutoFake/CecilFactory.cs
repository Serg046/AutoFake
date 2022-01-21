using System;
using DryIoc;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake
{
	internal class CecilFactory : ICecilFactory
	{
		private readonly IContainer _serviceLocator;

		public CecilFactory(IContainer serviceLocator) => _serviceLocator = serviceLocator;

		public VariableDefinition CreateVariable(TypeReference variableType)
		{
			return _serviceLocator.Resolve<Func<TypeReference, VariableDefinition>>().Invoke(variableType);
		}

		public ReaderParameters CreateReaderParameters()
		{
			return _serviceLocator.Resolve<ReaderParameters>();
		}

		public WriterParameters CreateWriterParameters()
		{
			return _serviceLocator.Resolve<WriterParameters>();
		}

		public ISymbolReaderProvider CreateSymbolReaderProvider(bool throwIfNoSymbol)
		{
			return _serviceLocator.Resolve<Func<bool, ISymbolReaderProvider>>().Invoke(throwIfNoSymbol);
		}

		public AssemblyNameDefinition CreateAssemblyNameDefinition(string name, Version version)
		{
			return _serviceLocator.Resolve<Func<string, Version, AssemblyNameDefinition>>().Invoke(name, version);
		}

		public TypeDefinition CreateTypeDefinition(string @namespace, string name, TypeAttributes attributes, TypeReference baseType)
		{
			return _serviceLocator.Resolve<Func<string, string, TypeAttributes, TypeReference, TypeDefinition>>()
				.Invoke(@namespace, @namespace, attributes, baseType);
		}
	}
}
