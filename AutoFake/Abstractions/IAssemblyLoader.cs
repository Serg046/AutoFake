using System;
using System.Reflection;

namespace AutoFake.Abstractions
{
	internal interface IAssemblyLoader
	{
		Tuple<Assembly, Type?> LoadAssemblies(IFakeOptions options, bool loadFieldsAsm);
	}
}