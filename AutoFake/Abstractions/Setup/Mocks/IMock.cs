using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Abstractions.Setup.Mocks
{
	internal interface IMock
	{
		bool IsSourceInstruction(MethodDefinition method, Instruction instruction, IEnumerable<GenericArgument> genericArguments);
		void BeforeInjection(MethodDefinition method);
		void Inject(IEmitter emitter, Instruction instruction);
		void AfterInjection(IEmitter emitter);
		void Initialize(Type? type);
	}
}
