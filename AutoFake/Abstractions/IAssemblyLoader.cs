using System;
using System.Reflection;

namespace AutoFake.Abstractions
{
	internal interface IAssemblyLoader
	{
		Tuple<Assembly, Type?> LoadAssemblies(DebugMode debugMode, bool loadFieldsAsm);
	}
}