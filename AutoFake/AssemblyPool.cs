using System;
using System.Collections.Generic;
using AutoFake.Abstractions;
using Mono.Cecil;

namespace AutoFake
{
	internal class AssemblyPool : IAssemblyPool
	{
		private readonly Func<ModuleDefinition, ITypeMap> _createTypeMap;
		private readonly List<ITypeMap> _typeMaps = new();
		private readonly Dictionary<string, ModuleDefinition> _modules = new();

		public AssemblyPool(Func<ModuleDefinition, ITypeMap> createTypeMap)
		{
			_createTypeMap = createTypeMap;
		}

		public bool TryAdd(ModuleDefinition module)
		{
			if (!_modules.ContainsKey(module.Assembly.FullName))
			{
				_modules.Add(module.Assembly.FullName, module);
				_typeMaps.Add(_createTypeMap(module));
				return true;
			}

			return false;
		}

		public bool HasModule(ModuleDefinition module) => _modules.ContainsKey(module.Assembly.FullName);

		public ICollection<ITypeMap> GetTypeMaps() => _typeMaps;

		public ICollection<ModuleDefinition> GetModules() => _modules.Values;
	}
}