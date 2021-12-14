using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using AutoFake.Exceptions;
using AutoFake.Setup;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake
{
	internal class AssemblyWriter : IAssemblyWriter
	{
        private const BindingFlags ConstructorFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		
        private readonly IAssemblyReader _assemblyReader;
		private readonly AssemblyNameReference _assemblyNameReference;
		private readonly Dictionary<string, ushort> _addedFields;
		private readonly IAssemblyHost _assemblyHost;
		private readonly FakeOptions _fakeOptions;
		private readonly IAssemblyPool _assemblyPool;

		public AssemblyWriter(IAssemblyReader assemblyReader, IAssemblyHost assemblyHost, FakeOptions fakeOptions, IAssemblyPool assemblyPool)
		{
			_assemblyReader = assemblyReader;
			_assemblyHost = assemblyHost;
			_fakeOptions = fakeOptions;
			_assemblyPool = assemblyPool;
			_assemblyNameReference = _assemblyReader.SourceTypeDefinition.Module.AssemblyReferences
	            .Single(a => a.FullName == _assemblyReader.SourceType.Assembly.FullName);
            _addedFields = new Dictionary<string, ushort>();

            foreach (var referencedType in _fakeOptions.ReferencedTypes)
            {
	            var typeRef = _assemblyReader.SourceTypeDefinition.Module.ImportReference(referencedType);
	            TryAddAffectedAssembly(typeRef.Resolve().Module.Assembly);
            }
		}

		public TypeReference ImportToSourceAsm(Type type)
			=> _assemblyReader.SourceTypeDefinition.Module.ImportReference(type);

		public FieldReference ImportToSourceAsm(FieldInfo field)
			=> _assemblyReader.SourceTypeDefinition.Module.ImportReference(field);

		public MethodReference ImportToSourceAsm(MethodBase method)
			=> _assemblyReader.SourceTypeDefinition.Module.ImportReference(method);

		public TypeReference ImportToSourceAsm(TypeReference type)
		{
			if (type.IsGenericParameter) return type;

			var result = NewTypeReference(type);
			var newType = result;
			while (type.DeclaringType != null)
			{
				type = type.DeclaringType;
				newType.DeclaringType = NewTypeReference(type);
				newType = newType.DeclaringType;
			}

			TypeReference NewTypeReference(TypeReference typeRef)
				=> new(typeRef.Namespace, typeRef.Name, _assemblyReader.SourceTypeDefinition.Module, _assemblyNameReference, typeRef.IsValueType);

			return result;
		}

		public TypeReference ImportToFieldsAsm(Type type)
			=> _assemblyReader.FieldsTypeDefinition.Module.ImportReference(type);

		public FieldReference ImportToFieldsAsm(FieldInfo field)
			=> _assemblyReader.FieldsTypeDefinition.Module.ImportReference(field);

		public MethodReference ImportToFieldsAsm(MethodBase method)
			=> _assemblyReader.FieldsTypeDefinition.Module.ImportReference(method);

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

		public FakeObjectInfo CreateFakeObject(MockCollection mocks, ICollection<FakeDependency> dependencies)
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
			foreach (var mock in mocks.SelectMany(m => m.Mocks))
			{
				mock.Initialize(fieldsType);
			}
			
			return new FakeObjectInfo(sourceType, fieldsType, instance);
		}

		private static string GetClrName(string monoCecilTypeName) => monoCecilTypeName.Replace('/', '+');

		private bool IsStatic(Type type) => type.IsAbstract && type.IsSealed;

		private object CreateInstance(Type type, ICollection<FakeDependency> dependencies)
		{
			if (dependencies.Any(d => d.Type == null))
			{
				try
				{
					var args = dependencies.Select(d => d.Instance).ToArray();
					return Activator.CreateInstance(type, ConstructorFlags, null, args, null)!;
				}
				catch (AmbiguousMatchException)
				{
					throw new InitializationException(
						$"Ambiguous null-invocation. Please use {nameof(Arg)}.{nameof(Arg.IsNull)}<T>() instead of null.");
				}
			}

			var constructor = type.GetConstructor(ConstructorFlags,
				null, dependencies.Select(d => d.Type!).ToArray(), null);

			if (constructor == null)
				throw new InitializationException("Constructor is not found");

			return constructor.Invoke(dependencies.Select(d => d.Instance).ToArray());
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
			var parameters = new WriterParameters();
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
