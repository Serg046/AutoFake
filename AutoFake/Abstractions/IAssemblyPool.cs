using System.Collections.Generic;
using Mono.Cecil;

namespace AutoFake.Abstractions
{
	internal interface IAssemblyPool
	{
		bool TryAdd(ModuleDefinition module);
		bool HasModule(ModuleDefinition module);
		ICollection<ITypeMap> GetTypeMaps();
		ICollection<ModuleDefinition> GetModules();
	}
}
