using System.Collections.Generic;
using AutoFake.Abstractions.Setup.Mocks;
using Mono.Cecil;

namespace AutoFake.Abstractions;

public interface IAssemblyWriter
{
	void AddField(FieldDefinition field);
	bool TryAddAffectedAssembly(AssemblyDefinition assembly);
	IFakeObjectInfo CreateFakeObject(IEnumerable<IMock> mocks, object?[] dependencies);
}
