using Mono.Cecil;

namespace AutoFake.Abstractions;

public interface IAssemblyWriter
{
	void AddField(FieldDefinition field);
	bool TryAddAffectedAssembly(AssemblyDefinition assembly);
	IFakeObjectInfo CreateFakeObject();
}
