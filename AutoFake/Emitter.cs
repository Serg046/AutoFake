using Mono.Cecil.Cil;

namespace AutoFake
{
    internal class Emitter : IEmitter
    {
        private readonly ILProcessor _processor;

        public Emitter(MethodBody body)
        {
            Body = body;
            _processor = body.GetILProcessor();
        }

        public MethodBody Body { get; }

        public void InsertBefore(Instruction target, Instruction instruction)
            => _processor.InsertBefore(target, instruction);

        public void InsertAfter(Instruction target, Instruction instruction)
            => _processor.InsertAfter(target, instruction);

        public void Replace(Instruction target, Instruction instruction)
            => _processor.Replace(target, instruction);

        public void Remove(Instruction instruction)
            => _processor.Remove(instruction);
    }
}
