﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using AutoFake.Exceptions;
using AutoFake.Setup.Mocks;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake
{
	internal class AssemblyWriter : IAssemblyWriter
	{
        private const BindingFlags ConstructorFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		
        private readonly IAssemblyReader _assemblyReader;
		private readonly Dictionary<string, ushort> _addedFields;
		private readonly IAssemblyHost _assemblyHost;
		private readonly FakeOptions _fakeOptions;
		private readonly IAssemblyPool _assemblyPool;
		private readonly ICecilFactory _cecilFactory;
		private readonly FakeObjectInfo.Create _createFakeObjectInfo;

		public AssemblyWriter(IAssemblyReader assemblyReader, IAssemblyHost assemblyHost, FakeOptions fakeOptions,
			IAssemblyPool assemblyPool, ICecilFactory cecilFactory, FakeObjectInfo.Create createFakeObjectInfo)
		{
			_assemblyReader = assemblyReader;
			_assemblyHost = assemblyHost;
			_fakeOptions = fakeOptions;
			_assemblyPool = assemblyPool;
			_cecilFactory = cecilFactory;
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
			using var stream = new MemoryStream();
			using var symbolsStream = new MemoryStream();

			LoadAffectedAssemblies();
			_assemblyReader.SourceTypeDefinition.Module.Write(stream, GetWriterParameters(symbolsStream, _assemblyReader.SourceTypeDefinition.Module.HasSymbols));
			var assembly = _assemblyHost.Load(stream, symbolsStream);
			var fieldsType = _addedFields.Count > 0
				? GetFieldsAssembly(assembly).GetType(_assemblyReader.FieldsTypeDefinition.FullName, true)
				: null; // No need for init, skipping

			var sourceType = assembly.GetType(GetClrName(_assemblyReader.SourceTypeDefinition.FullName), true)
				?? throw new InvalidOperationException("Cannot find a type");
			if (_assemblyReader.SourceType.IsGenericType)
			{
				sourceType = sourceType.MakeGenericType(_assemblyReader.SourceType.GetGenericArguments());
			}

			var instance = !IsStatic(_assemblyReader.SourceType) ? CreateInstance(sourceType, dependencies) : null;
			foreach (var mock in mocks)
			{
				mock.Initialize(fieldsType);
			}
			
			return _createFakeObjectInfo(sourceType, fieldsType, instance);
		}

		private static string GetClrName(string monoCecilTypeName) => monoCecilTypeName.Replace('/', '+');

		private bool IsStatic(Type type) => type.IsAbstract && type.IsSealed;

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

			if (!noTypeWrapper)
			{
				for (var i = 0; i < dependencies.Length; i++)
				{
					if (instances[i] is Arg.TypeWrapper w)
					{
						types[i] = w.Type;
						instances[i] = null;
					}
					else
					{
						types[i] = instances[i]?.GetType() ?? throw new InitializationException(
							$"Ambiguous null-invocation. Please use {nameof(Arg)}.{nameof(Arg.IsNull)}<T>() instead of null.");
					}
				}
			}

			if (noTypeWrapper)
			{
				try
				{
					return Activator.CreateInstance(type, ConstructorFlags, null, instances, null)!;
				}
				catch (AmbiguousMatchException)
				{
					throw new InitializationException(
						$"Ambiguous null-invocation. Please use {nameof(Arg)}.{nameof(Arg.IsNull)}<T>() instead of null.");
				}
			}

			var constructor = type.GetConstructor(ConstructorFlags, null, types, null) ?? throw new InitializationException("Constructor is not found");
			return constructor.Invoke(instances);
		}

		private void LoadAffectedAssemblies()
		{
			var asmNames = new Dictionary<string, string>();
			LoadAffectedAssembly(asmNames, _assemblyReader.SourceTypeDefinition.Module.Assembly);
			foreach (var affectedAssembly in _assemblyPool.GetModules().Select(m => m.Assembly))
			{
				LoadAffectedAssembly(asmNames, affectedAssembly);
			}

			foreach (var affectedAssembly in _assemblyPool.GetModules().Select(m => m.Assembly))
			{
				if (asmNames.TryGetValue(affectedAssembly.Name.Name, out var asmName))
				{
					affectedAssembly.Name.Name = asmName;
				}

				using var stream = new MemoryStream();
				using var symbolsStream = new MemoryStream();
				affectedAssembly.Write(stream, GetWriterParameters(symbolsStream, affectedAssembly.MainModule.HasSymbols));
				LoadRenamedAssembly(stream, symbolsStream, affectedAssembly);
			}
		}

		private void LoadAffectedAssembly(Dictionary<string, string> asmNames, AssemblyDefinition assembly)
		{
			foreach (var affectedAssembly in _assemblyPool.GetModules().Select(m => m.Assembly))
			foreach (var asmRef in assembly.MainModule.AssemblyReferences.Where(a => a.FullName == affectedAssembly.FullName))
			{
				if (!asmNames.TryGetValue(asmRef.Name, out var newAsmName))
				{
					var oldAsmName = asmRef.Name;
					newAsmName = oldAsmName + Guid.NewGuid();
					asmNames.Add(oldAsmName, newAsmName);
				}
				asmRef.Name = newAsmName;
			}
		}

		private Assembly LoadRenamedAssembly(MemoryStream stream, MemoryStream symbolsStream, AssemblyDefinition affectedAssembly)
		{
#if NETCOREAPP3_0
			return _assemblyHost.Load(stream, symbolsStream);
#else
			var assembly = _assemblyHost.Load(stream, symbolsStream);
			AppDomain.CurrentDomain.AssemblyResolve += (sender, args)
				=> args.Name == affectedAssembly.FullName ? assembly : null;
			return assembly;
#endif
		}

		private WriterParameters GetWriterParameters(MemoryStream symbolsStream, bool hasSymbols)
		{
			var parameters = _cecilFactory.CreateWriterParameters();
			if (_fakeOptions.Debug == DebugMode.Enabled ||
			    (_fakeOptions.Debug == DebugMode.Auto && Debugger.IsAttached && hasSymbols))
			{
				parameters.SymbolStream = symbolsStream;
				parameters.SymbolWriterProvider = new SymbolsWriterProvider();
			}

			return parameters;
		}

		private Assembly GetFieldsAssembly(Assembly executingAsm)
		{
			if (_assemblyReader.FieldsTypeDefinition.Module.Assembly != _assemblyReader.SourceTypeDefinition.Module.Assembly)
			{
				using var stream = new MemoryStream();
				using var symbolsStream = new MemoryStream();
				_assemblyReader.FieldsTypeDefinition.Module.Write(stream, GetWriterParameters(symbolsStream, _assemblyReader.FieldsTypeDefinition.Module.HasSymbols));
				return LoadRenamedAssembly(stream, symbolsStream, _assemblyReader.FieldsTypeDefinition.Module.Assembly);
			}

			return executingAsm;
		}

		private class SymbolsWriterProvider : ISymbolWriterProvider
		{
			public ISymbolWriter GetSymbolWriter(ModuleDefinition module, string fileName)
				=> throw new NotImplementedException();


			public ISymbolWriter GetSymbolWriter(ModuleDefinition module, Stream symbolStream)
			{
				if (!module.HasSymbols) throw new InvalidOperationException("There are no debug symbols");
				return module.SymbolReader.GetWriterProvider().GetSymbolWriter(module, symbolStream);
			}
		}
	}
}
