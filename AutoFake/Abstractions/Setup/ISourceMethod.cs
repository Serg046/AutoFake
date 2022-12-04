using Mono.Cecil;

namespace AutoFake.Abstractions.Setup;

public interface ISourceMethod : ISourceMember
{
	MethodDefinition GetMethod();
}
