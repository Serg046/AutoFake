using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoFake.Abstractions;
using Mono.Cecil;

namespace AutoFake;

internal class TypeInfo : ITypeInfo
{
	private readonly IAssemblyReader _assemblyReader;
	private readonly IFakeOptions _fakeOptions;
	private readonly IAssemblyPool _assemblyPool;
	private readonly ICecilFactory _cecilFactory;
	private readonly AssemblyNameReference _assemblyNameReference;

	public TypeInfo(IAssemblyReader assemblyReader, IFakeOptions fakeOptions, IAssemblyPool assemblyPool,
		ICecilFactory cecilFactory, Func<ModuleDefinition, ITypeMap> createTypeMap)
	{
		_assemblyReader = assemblyReader;
		_fakeOptions = fakeOptions;
		_assemblyPool = assemblyPool;
		_cecilFactory = cecilFactory;
		_assemblyNameReference = _assemblyReader.SourceTypeDefinition.Module.AssemblyReferences
			.Single(a => a.FullName == _assemblyReader.SourceType.Assembly.FullName);

		TypeMap = createTypeMap(_assemblyReader.SourceTypeDefinition.Module);
	}

	public bool IsMultipleAssembliesMode
		=> _fakeOptions.AnalysisLevel == AnalysisLevels.AllExceptSystemAndMicrosoft || _fakeOptions.ReferencedTypes.Count > 0;

	public Type SourceType => _assemblyReader.SourceType;

	public ITypeMap TypeMap { get; }

	public bool IsInFakeModule(TypeReference type)
		=> !type.IsGenericParameter && type.Scope == _assemblyReader.SourceTypeDefinition.Module || type.GenericParameters.Any(t => t.Scope == _assemblyReader.SourceTypeDefinition.Module);

	public TypeDefinition GetTypeDefinition(Type type) =>
		_assemblyReader.SourceTypeDefinition.Module.GetType(type.FullName, runtimeName: true).ToTypeDefinition();

	public IEnumerable<MethodDefinition> GetConstructors()
		=> _assemblyReader.SourceTypeDefinition.Methods.Where(m => m.Name is ".ctor" or ".cctor");

	public MethodDefinition? GetMethod(MethodBase method, bool searchInBaseType = false)
	{
		return GetMethod(_assemblyReader.SourceTypeDefinition, method, searchInBaseType);
	}

	public MethodDefinition? GetMethod(TypeDefinition type, MethodBase method, bool searchInBaseType = false)
	{
		var methodRef = ImportToSourceAsm(method);
		if (method.DeclaringType?.IsInterface == true)
		{
			methodRef = FilterGenericArguments(methodRef);
			methodRef.Name = method.GetFullMethodName();
			methodRef.DeclaringType = _assemblyReader.SourceTypeDefinition;
		}

		return GetMethod(type, methodRef, searchInBaseType);
	}

	private MethodDefinition? GetMethod(TypeDefinition type, MethodReference methodReference, bool searchInBaseType = false)
	{
		methodReference = FilterGenericArguments(methodReference);
		var method = type.Methods.SingleOrDefault(m => m.ToString() == methodReference.ToString());
		if (searchInBaseType && method == null && type.BaseType != null)
		{
			return GetMethod(type.BaseType.ToTypeDefinition(), methodReference, searchInBaseType);
		}

		return method;
	}

	private MethodReference FilterGenericArguments(MethodReference method)
	{
		if (method is GenericInstanceMethod genericMethod)
		{
			method = genericMethod.GetElementMethod();
		}

		if (method.DeclaringType is GenericInstanceType genericType)
		{
			method.DeclaringType = genericType.GetElementType();
		}

		return method;
	}

	public ICollection<MethodDefinition> GetAllImplementations(MethodDefinition method, bool includeAffectedAssemblies = false)
	{
		var methods = new HashSet<MethodDefinition>();
		IEnumerable<ITypeMap> typeMaps = new[] { TypeMap };
		if (includeAffectedAssemblies) typeMaps = typeMaps.Concat(_assemblyPool.GetTypeMaps());
		foreach (var typeDef in typeMaps.SelectMany(m => m.GetAllParentsAndDescendants(method.DeclaringType)))
		{
			foreach (var methodDef in typeDef.Methods.Where(m => Equivalent(m, method)))
			{
				methods.Add(methodDef);
			}
		}

		return methods;
	}

	private static bool Equivalent(MethodReference methodReference, MethodReference method)
		=> (methodReference.Name == method.Name || methodReference.Name.EndsWith($".{method.Name}")) &&
		   methodReference.Parameters.Select(p => p.ParameterType.FullName)
		   .SequenceEqual(method.Parameters.Select(p => p.ParameterType.FullName)) &&
		   methodReference.ReturnType.FullName == method.ReturnType.FullName;

	public bool IsInReferencedAssembly(AssemblyDefinition assembly) => _assemblyPool.HasModule(assembly.MainModule);

	public TypeReference ImportToSourceAsm(Type type)
		=> _assemblyReader.SourceTypeDefinition.Module.ImportReference(type);

	public FieldReference ImportToSourceAsm(FieldInfo field)
		=> _assemblyReader.SourceTypeDefinition.Module.ImportReference(field);

	public MethodReference ImportToSourceAsm(MethodBase method)
		=> _assemblyReader.SourceTypeDefinition.Module.ImportReference(method);

	public TypeReference ImportToSourceAsm(TypeReference type)
	{
		var result = CreateTypeReference(type);
		var newType = result;
		while (type.DeclaringType != null)
		{
			type = type.DeclaringType;
			newType.GetElementType().DeclaringType = CreateTypeReference(type);
			newType = newType.DeclaringType;
		}

		return result;
	}

	private TypeReference CreateTypeReference(TypeReference typeRef)
	{
		var newTypeRef = _cecilFactory.CreateTypeReference(typeRef.Namespace, typeRef.Name, _assemblyReader.SourceTypeDefinition.Module, _assemblyNameReference, typeRef.IsValueType);
		if (typeRef is GenericInstanceType genericInstanceType)
		{
			var newGenericInstanceType = _cecilFactory.CreateGenericInstanceType(newTypeRef);
			foreach (var arg in genericInstanceType.GenericArguments)
			{
				newGenericInstanceType.GenericArguments.Add(IsInFakeModule(arg) ? ImportToSourceAsm(arg) : arg);
			}

			return newGenericInstanceType;
		}

		return newTypeRef;
	}

	public MethodReference ImportToSourceAsm(MethodReference originalMethodRef)
	{
		var declaringType = ImportToSourceAsm(originalMethodRef.DeclaringType);
		var newMethodRef = _cecilFactory.CreateMethodReference(originalMethodRef.Name, originalMethodRef.ReturnType, declaringType);
		newMethodRef.CallingConvention = originalMethodRef.CallingConvention;
		newMethodRef.HasThis = originalMethodRef.HasThis;
		newMethodRef.ExplicitThis = originalMethodRef.ExplicitThis;

		foreach (var paramDef in originalMethodRef.Parameters)
		{
			newMethodRef.Parameters.Add(_cecilFactory.CreateParameterDefinition(paramDef.Name, paramDef.Attributes, paramDef.ParameterType));
		}

		foreach (var paramDef in originalMethodRef.GetElementMethod().GenericParameters)
		{
			newMethodRef.GenericParameters.Add(_cecilFactory.CreateGenericParameter(paramDef.Name, newMethodRef));
		}

		if (originalMethodRef is GenericInstanceMethod genericInstanceMethod)
		{
			var newGenericInstanceMethod = _cecilFactory.CreateGenericInstanceMethod(newMethodRef);
			foreach (var arg in genericInstanceMethod.GenericArguments)
			{
				newGenericInstanceMethod.GenericArguments.Add(arg);
			}

			return newGenericInstanceMethod;
		}

		return newMethodRef;
	}

	public TypeReference ImportToFieldsAsm(Type type)
		=> _assemblyReader.FieldsTypeDefinition.Module.ImportReference(type);
}
