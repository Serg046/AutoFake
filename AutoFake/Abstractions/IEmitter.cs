using Mono.Cecil.Cil;

namespace AutoFake.Abstractions;

public interface IEmitter
{
    MethodBody Method { get; }
    Instruction BaseInstruction { get; }
    void InsertAbove(Instruction instruction);
    void InsertBelow(Instruction instruction);
}