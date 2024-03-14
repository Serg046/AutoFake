using System;
using System.Collections.Generic;
using AutoFake.Abstractions;
using Mono.Cecil;

namespace AutoFake;

internal class AssemblyWriter : IAssemblyWriter
{
	private readonly IAssemblyReader _assemblyReader;
	private readonly Dictionary<string, ushort> _addedFields;
	private readonly IOptions _options;
	private readonly IAssemblyPool _assemblyPool;
	private readonly IAssemblyLoader _assemblyLoader;
	private readonly FakeObjectInfo.Create _createFakeObjectInfo;

	public AssemblyWriter(IAssemblyReader assemblyReader, IOptions options,
		IAssemblyPool assemblyPool, IAssemblyLoader assemblyLoader, FakeObjectInfo.Create createFakeObjectInfo)
	{
		_assemblyReader = assemblyReader;
		_options = options;
		_assemblyPool = assemblyPool;
		_assemblyLoader = assemblyLoader;
		_createFakeObjectInfo = createFakeObjectInfo;

		_addedFields = new Dictionary<string, ushort>();

		foreach (var referencedType in _options.ReferencedTypes)
		{
			var typeRef = _assemblyReader.SourceTypeDefinition.Module.ImportReference(referencedType);
			TryAddAffectedAssembly(typeRef.Resolve().Module.Assembly);
		}
	}

	public void AddField(FieldDefinition field)
	{
		if (!_addedFields.ContainsKey(field.Name))
		{
			_assemblyReader.FieldsTypeDefinition.Fields.Add(field);
			_addedFields.Add(field.Name, 0);
		}
		else
		{
			_addedFields[field.Name]++;
			field.Name += _addedFields[field.Name];
			_assemblyReader.FieldsTypeDefinition.Fields.Add(field);
		}
	}

	public bool TryAddAffectedAssembly(AssemblyDefinition assembly) => _assemblyPool.TryAdd(assembly.MainModule);

	public IFakeObjectInfo CreateFakeObject()
	{
		var loader = _assemblyLoader.LoadAssemblies(_options, loadFieldsAsm: _addedFields.Count > 0);
		var sourceType = loader.Item1.GetType(_assemblyReader.SourceType.FullName) ?? throw new InvalidOperationException("Cannot find a type");
		if (_assemblyReader.SourceType.IsGenericType)
		{
			sourceType = sourceType.MakeGenericType(_assemblyReader.SourceType.GetGenericArguments());
		}

		var instance = !_assemblyReader.SourceType.IsStatic() ? Activator.CreateInstance(sourceType) : null;
		return _createFakeObjectInfo(sourceType, instance);
	}
}
