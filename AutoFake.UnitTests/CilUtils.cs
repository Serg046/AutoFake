using System;
using System.Collections.Generic;
using System.Linq;
using GuardExtensions;
using Mono.Cecil.Cil;

namespace AutoFake.UnitTests
{
    internal static class CilUtils
    {
        public static bool Ordered(this IEnumerable<Instruction> instructions, params OpCode[] opCodes)
        {
            Guard.IsNotNull(opCodes);
            Guard.IsPositive(opCodes.Length);

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
            Guard.IsNotNull(cilCmds);
            Guard.IsPositive(cilCmds.Length);

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
