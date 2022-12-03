using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Abstractions.Setup.Mocks;

public interface ISourceMemberInsertMockInjector
{
	void Inject(IEmitter emitter, Instruction instruction, FieldReference closureField, IInsertMock.Location location);
}
