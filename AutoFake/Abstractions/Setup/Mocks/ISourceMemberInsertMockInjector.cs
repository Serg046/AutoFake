using AutoFake.Setup.Mocks;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Abstractions.Setup.Mocks
{
	internal interface ISourceMemberInsertMockInjector
	{
		void Inject(IEmitter emitter, Instruction instruction, FieldReference closureField, InsertMock.Location location);
	}
}