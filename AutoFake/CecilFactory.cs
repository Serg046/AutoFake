using System;
using AutoFake.Abstractions;
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

		public MethodReference CreateMethodReference(string name, TypeReference returnType, TypeReference declaringType)
		{
			return _serviceLocator.Resolve<Func<string, TypeReference, TypeReference, MethodReference>>()
				.Invoke(name, returnType, declaringType);
		}

		public ParameterDefinition CreateParameterDefinition(string name, ParameterAttributes attributes, TypeReference parameterType)
		{
			return _serviceLocator.Resolve<Func<string, ParameterAttributes, TypeReference, ParameterDefinition>>()
				.Invoke(name, attributes, parameterType);
		}

		public GenericParameter CreateGenericParameter(string name, IGenericParameterProvider owner)
		{
			return _serviceLocator.Resolve<Func<string, IGenericParameterProvider, GenericParameter>>()
				.Invoke(name, owner);
		}

		public FieldDefinition CreateFieldDefinition(string name, FieldAttributes attributes, TypeReference fieldType)
		{
			return _serviceLocator.Resolve<Func<string, FieldAttributes, TypeReference, FieldDefinition >>()
				.Invoke(name, attributes, fieldType);
		}

		public GenericInstanceMethod CreateGenericInstanceMethod(MethodReference method)
		{
			return _serviceLocator.Resolve<Func<MethodReference, GenericInstanceMethod>>().Invoke(method);
		}

		public TypeReference CreateTypeReference(string @namespace, string name, ModuleDefinition module, IMetadataScope scope, bool valueType)
		{
			return _serviceLocator.Resolve<Func<string, string, ModuleDefinition, IMetadataScope, bool, TypeReference>>()
				.Invoke(@namespace, name, module, scope, valueType);
		}
	}
}
