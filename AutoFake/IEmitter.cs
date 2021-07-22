using Mono.Cecil.Cil;

namespace AutoFake
{
    internal interface IEmitter
    {
        MethodBody Body { get; }
        void InsertBefore(Instruction target, Instruction instruction);
        void InsertAfter(Instruction target, Instruction instruction);
        void Remove(Instruction instruction);
    }
}
