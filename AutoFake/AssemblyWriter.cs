using System;
using System.Collections.Generic;
using System.Reflection;
using AutoFake.Abstractions;
using AutoFake.Abstractions.Setup.Mocks;
using AutoFake.Exceptions;
using Mono.Cecil;

namespace AutoFake
{
	internal class AssemblyWriter : IAssemblyWriter
	{
		private const BindingFlags ConstructorFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

		private readonly IAssemblyReader _assemblyReader;
		private readonly Dictionary<string, ushort> _addedFields;
		private readonly IFakeOptions _fakeOptions;
		private readonly IAssemblyPool _assemblyPool;
		private readonly IAssemblyLoader _assemblyLoader;
		private readonly FakeObjectInfo.Create _createFakeObjectInfo;

		public AssemblyWriter(IAssemblyReader assemblyReader, IFakeOptions fakeOptions,
			IAssemblyPool assemblyPool, IAssemblyLoader assemblyLoader, FakeObjectInfo.Create createFakeObjectInfo)
		{
			_assemblyReader = assemblyReader;
			_fakeOptions = fakeOptions;
			_assemblyPool = assemblyPool;
			_assemblyLoader = assemblyLoader;
			_createFakeObjectInfo = createFakeObjectInfo;

			_addedFields = new Dictionary<string, ushort>();

			foreach (var referencedType in _fakeOptions.ReferencedTypes)
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

		public FakeObjectInfo CreateFakeObject(IEnumerable<IMock> mocks, object?[] dependencies)
		{
			var loader = _assemblyLoader.LoadAssemblies(_fakeOptions, loadFieldsAsm: _addedFields.Count > 0);
			var sourceType = loader.Item1.GetType(GetClrName(_assemblyReader.SourceTypeDefinition.FullName)) ?? throw new InvalidOperationException("Cannot find a type");
			if (_assemblyReader.SourceType.IsGenericType)
			{
				sourceType = sourceType.MakeGenericType(_assemblyReader.SourceType.GetGenericArguments());
			}

			foreach (var mock in mocks)
			{
				mock.Initialize(loader.Item2);
			}

			var instance = !_assemblyReader.SourceType.IsStatic() ? CreateInstance(sourceType, dependencies) : null;
			return _createFakeObjectInfo(sourceType, loader.Item2, instance);
		}

		private static string GetClrName(string monoCecilTypeName) => monoCecilTypeName.Replace('/', '+');

		private object CreateInstance(Type type, object?[] dependencies)
		{
			var types = new Type[dependencies.Length];
			var instances = new object?[dependencies.Length];
			var noTypeWrapper = true;
			for (var i = 0; i < dependencies.Length; i++)
			{
				instances[i] = dependencies[i];
				if (instances[i] is Arg.TypeWrapper)
				{
					noTypeWrapper = false;
				}
			}

			if (noTypeWrapper) return ExecuteViaActivator(type, instances);

			FillDependencyTypes(dependencies.Length, instances, types);
			var constructor = type.GetConstructor(ConstructorFlags, null, types, null) ?? throw new InitializationException("Constructor is not found");
			return constructor.Invoke(instances);
		}

		private static void FillDependencyTypes(int numberOfDependencies, object?[] dependencies, Type[] types)
		{
			for (var i = 0; i < numberOfDependencies; i++)
			{
				if (dependencies[i] is Arg.TypeWrapper w)
				{
					types[i] = w.Type;
					dependencies[i] = null;
				}
				else
				{
					types[i] = dependencies[i]?.GetType() ?? throw new InitializationException(
						$"Ambiguous null-invocation. Please use {nameof(Arg)}.{nameof(Arg.IsNull)}<T>() instead of null.");
				}
			}
		}

		private static object ExecuteViaActivator(Type type, object?[] dependencies)
		{
			try
			{
				return Activator.CreateInstance(type, ConstructorFlags, null, dependencies, null)!;
			}
			catch (AmbiguousMatchException)
			{
				throw new InitializationException(
					$"Ambiguous null-invocation. Please use {nameof(Arg)}.{nameof(Arg.IsNull)}<T>() instead of null.");
			}
		}
	}
}
