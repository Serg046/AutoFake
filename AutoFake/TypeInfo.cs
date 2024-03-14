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
	private readonly IAssemblyPool _assemblyPool;
	private readonly Lazy<ITypeMap> _typeMap;

	public TypeInfo(IAssemblyReader assemblyReader, IAssemblyPool assemblyPool, Func<ModuleDefinition, ITypeMap> createTypeMap)
	{
		_assemblyReader = assemblyReader;
		_assemblyPool = assemblyPool;
		_typeMap = new(() => createTypeMap(_assemblyReader.SourceTypeDefinition.Module));
	}

	public Type SourceType => _assemblyReader.SourceType;

	public ITypeMap TypeMap => _typeMap.Value;

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

	public TypeReference ImportToFieldsAsm(Type type)
		=> _assemblyReader.FieldsTypeDefinition.Module.ImportReference(type);
}
