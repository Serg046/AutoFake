using System.Collections.Generic;
using Mono.Cecil;

namespace AutoFake.Abstractions;

public interface IAssemblyPool
{
	bool TryAdd(ModuleDefinition module);
	bool HasModule(ModuleDefinition module);
	ICollection<ITypeMap> GetTypeMaps();
	ICollection<ModuleDefinition> GetModules();
}
