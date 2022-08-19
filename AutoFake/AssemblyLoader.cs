using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using AutoFake.Abstractions;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake
{
	internal class AssemblyLoader : IAssemblyLoader
	{
		private readonly IAssemblyReader _assemblyReader;
		private readonly IAssemblyHost _assemblyHost;
		private readonly IAssemblyPool _assemblyPool;
		private readonly ICecilFactory _cecilFactory;

		public AssemblyLoader(IAssemblyReader assemblyReader, IAssemblyHost assemblyHost,
			IAssemblyPool assemblyPool, ICecilFactory cecilFactory)
		{
			_assemblyReader = assemblyReader;
			_assemblyHost = assemblyHost;
			_assemblyPool = assemblyPool;
			_cecilFactory = cecilFactory;
		}

		public Tuple<Assembly, Type?> LoadAssemblies(IFakeOptions options, bool loadFieldsAsm)
		{
			using var stream = new MemoryStream();
			using var symbolsStream = new MemoryStream();

			LoadAffectedAssemblies(options);
			_assemblyReader.SourceTypeDefinition.Module.Write(stream, GetWriterParameters(symbolsStream, options));
			var assembly = _assemblyHost.Load(stream, symbolsStream);
			var fieldsType = loadFieldsAsm
				? GetFieldsAssembly(assembly, options).GetType(_assemblyReader.FieldsTypeDefinition.FullName, true)
				: null;
			return new(assembly, fieldsType);
		}

		private void LoadAffectedAssemblies(IFakeOptions options)
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
				affectedAssembly.Write(stream, GetWriterParameters(symbolsStream, options));
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
			AppDomain.CurrentDomain.AssemblyResolve += (sender, args) => args.Name == affectedAssembly.FullName ? assembly : null;
			return assembly;
#endif
		}

		private WriterParameters GetWriterParameters(MemoryStream symbolsStream, IFakeOptions options)
		{
			var parameters = _cecilFactory.CreateWriterParameters();
			if (options.IsDebugEnabled)
			{
				parameters.SymbolStream = symbolsStream;
				parameters.SymbolWriterProvider = new SymbolsWriterProvider();
			}

			return parameters;
		}

		private Assembly GetFieldsAssembly(Assembly executingAsm, IFakeOptions options)
		{
			if (_assemblyReader.FieldsTypeDefinition.Module.Assembly != _assemblyReader.SourceTypeDefinition.Module.Assembly)
			{
				using var stream = new MemoryStream();
				using var symbolsStream = new MemoryStream();
				_assemblyReader.FieldsTypeDefinition.Module.Write(stream, GetWriterParameters(symbolsStream, options));
				return LoadRenamedAssembly(stream, symbolsStream, _assemblyReader.FieldsTypeDefinition.Module.Assembly);
			}

			return executingAsm;
		}

		private class SymbolsWriterProvider : ISymbolWriterProvider
		{
			[ExcludeFromCodeCoverage]
			public ISymbolWriter GetSymbolWriter(ModuleDefinition module, string fileName) => throw new NotImplementedException();

			public ISymbolWriter? GetSymbolWriter(ModuleDefinition module, Stream symbolStream)
			{
				return module.HasSymbols ? module.SymbolReader.GetWriterProvider().GetSymbolWriter(module, symbolStream) : null;
			}
		}
	}
}
