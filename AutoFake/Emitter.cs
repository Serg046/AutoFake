using AutoFake.Abstractions;
using Mono.Cecil.Cil;

namespace AutoFake;

internal class Emitter(MethodBody method, Instruction baseInstruction) : IEmitter
{
    private readonly ILProcessor _processor = method.GetILProcessor();
    public MethodBody Method => method;
    public Instruction BaseInstruction => baseInstruction;
    public void Emit(Instruction instruction) => _processor.InsertBefore(baseInstruction, instruction);
    public void InsertBelow(Instruction instruction) => _processor.InsertAfter(baseInstruction, instruction);
}