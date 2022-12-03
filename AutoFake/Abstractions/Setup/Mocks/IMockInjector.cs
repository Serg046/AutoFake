using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;

namespace AutoFake.Abstractions.Setup.Mocks;

public interface IMockInjector
{
	bool IsSourceInstruction(MethodDefinition method, Instruction instruction, IEnumerable<IGenericArgument> genericArguments);
	void Inject(IEmitter emitter, Instruction instruction);
}
