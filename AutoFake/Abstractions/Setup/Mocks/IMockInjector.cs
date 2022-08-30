using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;

namespace AutoFake.Abstractions.Setup.Mocks
{
	internal interface IMockInjector
	{
		bool IsSourceInstruction(MethodDefinition method, Instruction instruction, IEnumerable<GenericArgument> genericArguments);
		void Inject(IEmitter emitter, Instruction instruction);
	}
}
