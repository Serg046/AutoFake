using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Cil;

namespace AutoFake.UnitTests.TestUtils
{
    internal static class CilUtils
    {
        public static bool Ordered(this IEnumerable<Instruction> instructions, params OpCode[] opCodes)
        {
            return Ordered(instructions, (IList<OpCode>)opCodes);
        }

        public static bool Ordered(this IEnumerable<Instruction> instructions, IList<OpCode> opCodes)
        {
            if (opCodes == null || opCodes.Count < 1)
                throw new ArgumentException("opCodes is empty");

            Func<Instruction, bool> filter = i => i.OpCode != opCodes[0];
            var tmp = instructions.SkipWhile(filter);

            while (tmp.Any())
            {
                if (tmp.Take(opCodes.Count).Select(i => i.OpCode).SequenceEqual(opCodes))
                {
                    return true;
                }
                tmp = tmp.Skip(1).SkipWhile(filter);
            }

            return false;
        }

        public static bool Ordered(this IEnumerable<Instruction> instructions, params Cil[] cilCmds)
        {
            return Ordered(instructions, (IList<Cil>) cilCmds);
        }

        public static bool Ordered(this IEnumerable<Instruction> instructions, IList<Cil> cilCmds)
        {
            if (cilCmds == null || cilCmds.Count < 1)
                throw new ArgumentException("cilCmds is empty");

            Func<Instruction, bool> filter = i => i.OpCode != cilCmds[0].OpCode && i.Operand != cilCmds[0].Operand;
            var tmp = instructions.SkipWhile(filter);

            while (tmp.Any())
            {
                if (cilCmds.SequenceEqual(tmp.Take(cilCmds.Count).Select(i => Cil.Cmd(i.OpCode, i.Operand))))
                {
                    return true;
                }
                tmp = tmp.Skip(1).SkipWhile(filter);
            }

            return false;
        }
    }
}
