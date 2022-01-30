using System.Collections.Generic;
using AutoFake.Abstractions.Setup.Mocks;
using Mono.Cecil;

namespace AutoFake.Abstractions
{
	internal interface IAssemblyWriter
	{
		void AddField(FieldDefinition field);
		bool TryAddAffectedAssembly(AssemblyDefinition assembly);
		FakeObjectInfo CreateFakeObject(IEnumerable<IMock> mocks, object?[] dependencies);
	}
}