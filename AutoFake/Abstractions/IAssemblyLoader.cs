using System;
using System.Reflection;

namespace AutoFake.Abstractions;

public interface IAssemblyLoader
{
	Tuple<Assembly, Type?> LoadAssemblies(IFakeOptions options, bool loadFieldsAsm);
}
