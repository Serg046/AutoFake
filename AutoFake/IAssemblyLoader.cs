using System;
using System.Reflection;

namespace AutoFake
{
	internal interface IAssemblyLoader
	{
		Tuple<Assembly, Type?> LoadAssemblies(DebugMode debugMode, bool loadFieldsAsm);
	}
}