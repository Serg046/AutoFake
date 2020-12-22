using System.Linq;
using FluentAssertions;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using Xunit;

namespace AutoFake.UnitTests
{
	public class EmitterPoolTests
	{
		[Theory, AutoMoqData]
		internal void GetEmitter_TwoRequests_TheSameInstance(
			MethodBody body,
			EmitterPool sut)
		{
			var first = sut.GetEmitter(body);
			var second = sut.GetEmitter(body);

			first.Should().BeSameAs(second);
		}

		[Theory, AutoMoqData]
		internal void GetEmitter_Body_SimplifyMacrosCalled(
			MethodBody body,
			EmitterPool sut)
		{
			body.Instructions.Clear();
			body.Instructions.Add(Instruction.Create(OpCodes.Brfalse_S, Instruction.Create(OpCodes.Nop)));

			sut.GetEmitter(body);

			body.Instructions.Single().OpCode.Should().Be(OpCodes.Brfalse);
		}

		[Theory, AutoMoqData]
		internal void Dispose_Body_SimplifyMacrosCalled(
			MethodBody body,
			EmitterPool sut)
		{
			body.Instructions.Clear();
			body.Instructions.Add(Instruction.Create(OpCodes.Brfalse, Instruction.Create(OpCodes.Nop)));

			sut.GetEmitter(body);
			sut.Dispose();

			body.Instructions.Single().OpCode.Should().Be(OpCodes.Brfalse_S);
		}
	}
}
