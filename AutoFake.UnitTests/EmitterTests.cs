using System.Collections.Generic;
using System.Linq;
using AutoFixture.Xunit2;
using Mono.Cecil.Cil;
using Xunit;

namespace AutoFake.UnitTests
{
    public class EmitterTests
    {
        [Theory, AutoMoqData]
        internal void Body_ValidData_Success(MethodBody method)
        {
            var emitter = new Emitter(method);

            Assert.Equal(method, emitter.Body);
        }

        [Theory, AutoMoqData]
        internal void InsertBefore_ValidData_Injected(
            [Frozen]MethodBody method,
            Instruction cmd1, Instruction cmd2,
            Emitter sut)
        {
            method.Instructions.Add(cmd1);

            sut.InsertBefore(cmd1, cmd2);

            Assert.NotEqual(cmd1, cmd2);
            Assert.Equal<IEnumerable<Instruction>>(new[] { cmd2, cmd1 },
                method.Instructions.ToArray());
        }

        [Theory, AutoMoqData]
        internal void InsertAfter_ValidData_Injected(
            [Frozen]MethodBody method,
            Instruction cmd1, Instruction cmd2,
            Emitter sut)
        {
            method.Instructions.Add(cmd1);

            sut.InsertAfter(cmd1, cmd2);

            Assert.NotEqual(cmd1, cmd2);
            Assert.Equal<IEnumerable<Instruction>>(new[] { cmd1, cmd2 },
                method.Instructions.ToArray());
        }

        [Theory, AutoMoqData]
        internal void Replace_ValidData_Injected(
            [Frozen]MethodBody method,
            Instruction cmd1, Instruction cmd2,
            Emitter sut)
        {
            method.Instructions.Add(cmd1);

            sut.Replace(cmd1, cmd2);

            Assert.NotEqual(cmd1, cmd2);
            Assert.Equal(cmd2, method.Instructions.Single());
        }

        [Theory, AutoMoqData]
        internal void Remove_ValidData_Removed(
            [Frozen]MethodBody method,
            Instruction cmd,
            Emitter sut)
        {
            method.Instructions.Add(cmd);

            sut.Remove(cmd);

            Assert.Empty(method.Instructions);
        }
    }
}
