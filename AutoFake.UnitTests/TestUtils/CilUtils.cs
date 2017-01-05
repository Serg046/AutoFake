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
            if (opCodes == null || opCodes.Length < 1)
                throw new ArgumentException("opCodes is empty");

            Func<Instruction, bool> filter = i => i.OpCode != opCodes[0];
            var tmp = instructions.SkipWhile(filter);

            while (tmp.Any())
            {
                if (tmp.Take(opCodes.Length).Select(i => i.OpCode).SequenceEqual(opCodes))
                {
                    return true;
                }
                tmp = tmp.Skip(1).SkipWhile(filter);
            }

            return false;
        }

        public static bool Ordered(this IEnumerable<Instruction> instructions, params Cil[] cilCmds)
        {
            if (cilCmds == null || cilCmds.Length < 1)
                throw new ArgumentException("cilCmds is empty");

            Func<Instruction, bool> filter = i => i.OpCode != cilCmds[0].OpCode && i.Operand != cilCmds[0].Operand;
            var tmp = instructions.SkipWhile(filter);

            while (tmp.Any())
            {
                if (cilCmds.SequenceEqual(tmp.Take(cilCmds.Length).Select(i => Cil.Cmd(i.OpCode, i.Operand))))
                {
                    return true;
                }
                tmp = tmp.Skip(1).SkipWhile(filter);
            }

            return false;
        }
    }
}
