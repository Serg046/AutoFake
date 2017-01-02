using System;
using Mono.Cecil.Cil;

namespace AutoFake.UnitTests.TestUtils
{
#pragma warning disable CS0659
    internal class Cil
    {
        private readonly bool _isDelegate;
        private readonly bool _isAny;

        private Cil(OpCode opCode, object operand, bool isDelegate = false, bool isAny = false)
        {
            _isDelegate = isDelegate;
            _isAny = isAny;
            OpCode = opCode;
            Operand = operand;
        }

        public static Cil Cmd(OpCode opCode) => new Cil(opCode, null);
        public static Cil Cmd(OpCode opCode, object operand) => new Cil(opCode, operand);
        public static Cil Cmd<T>(OpCode opCode, Func<T, bool> operand) => new Cil(opCode, operand, true);

        public static Cil AnyCmd() => new Cil(OpCodes.Nop, null, isAny: true);

        public OpCode OpCode { get; }
        public object Operand { get; }

        public override bool Equals(object obj)
        {
            if (_isAny)
                return true;

            var o = (Cil)obj;

            return _isDelegate
                ? ((dynamic)this.Operand)((dynamic)o.Operand)
                : this.OpCode.Equals(o.OpCode) && object.Equals(Operand, o.Operand);
        }
    }
}
