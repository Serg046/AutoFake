using System.Collections.Generic;
using System.Linq;
using AutoFake.UnitTests.TestUtils;
using AutoFixture;
using AutoFixture.Xunit2;
using Mono.Cecil.Cil;
using Xunit;
using Mono.Cecil;

namespace AutoFake.UnitTests
{
    public class ProcessorTests
    {
        [Theory, AutoMoqData]
        internal void SaveMethodCall_MethodWithTwoArgs_ReturnsTwoVariables(
            MethodDefinition method, FieldDefinition accumulator,
            IFixture fixture)
        {
            method.Parameters.Add(new ParameterDefinition(new FunctionPointerType()));
            method.Parameters.Add(new ParameterDefinition(new FunctionPointerType()));
            fixture.Inject(Instruction.Create(OpCodes.Call, method));

            var variables = fixture.Create<Processor>().SaveMethodCall(accumulator, false);

            Assert.Equal(2, variables.Count);
        }

        [Theory, AutoMoqData]
        internal void SaveMethodCall_CheckArgs_ArgsSaved(
            [Frozen(Matching.ImplementedInterfaces)]Emitter emitter,
            TypeDefinition refType,
            MethodDefinition method, FieldDefinition accumulator,
            IFixture fixture)
        {
            method.Parameters.Add(new ParameterDefinition(TypeExtensions.ValueTypeDef()));
            method.Parameters.Add(new ParameterDefinition(refType));
            var instruction = Instruction.Create(OpCodes.Call, method);
            emitter.Body.Instructions.Add(instruction);
            fixture.Inject(instruction);

            var variables = fixture.Create<Processor>().SaveMethodCall(accumulator, true);

            var arrVar = emitter.Body.Variables.Single(v => v.VariableType.FullName == "System.Object[]");
            Assert.True(emitter.Body.Instructions.Ordered(
                Cil.Cmd(OpCodes.Ldloc, arrVar),
                Cil.Cmd(OpCodes.Ldc_I4, 0),
                Cil.Cmd(OpCodes.Ldloc, variables[0]),
                Cil.Cmd(OpCodes.Box, variables[0].VariableType),
                Cil.Cmd(OpCodes.Stelem_Ref),
                Cil.Cmd(OpCodes.Ldloc, arrVar),
                Cil.Cmd(OpCodes.Ldc_I4, 1),
                Cil.Cmd(OpCodes.Ldloc, variables[1]),
                Cil.Cmd(OpCodes.Stelem_Ref),
                Cil.AnyCmd(),
                Cil.AnyCmd(),
                Cil.AnyCmd(),
                Cil.Cmd(instruction.OpCode, instruction.Operand)
            ));
        }

        [Theory, AutoMoqData]
        internal void RemoveMethodArgumentsIfAny_ValidInput_InjectedArgumentsRemoving(
            [Frozen(Matching.ImplementedInterfaces)]Emitter emitter,
            [Frozen]MethodDefinition method,
            ParameterDefinition param1, ParameterDefinition param2,
            IFixture fixture)
        {
            method.Parameters.Add(param1);
            method.Parameters.Add(param2);
            var cmd = Instruction.Create(OpCodes.Call, method);
            emitter.Body.Instructions.Add(cmd);
            fixture.Inject(cmd);

            fixture.Create<Processor>().RemoveMethodArgumentsIfAny();

            Assert.True(emitter.Body.Instructions.Ordered(
                Cil.Cmd(OpCodes.Pop),
                Cil.Cmd(OpCodes.Pop),
                Cil.Cmd(cmd.OpCode, cmd.Operand)
                ));
        }

        [Theory, AutoMoqData]
        internal void RemoveMethodArgumentsIfAny_Field_NothingInjected(
            [Frozen(Matching.ImplementedInterfaces)]Emitter emitter,
            FieldDefinition field,
            IFixture fixture)
        {
            var cmd = Instruction.Create(OpCodes.Ldfld, field);
            emitter.Body.Instructions.Add(cmd);

            fixture.Create<Processor>().RemoveMethodArgumentsIfAny();

            Assert.Equal(cmd, emitter.Body.Instructions.Single());
        }

        [Theory, AutoMoqData]
        internal void RemoveStackArgument_ValidInput_InjectedStackArgumentRemoving(
            [Frozen(Matching.ImplementedInterfaces)]Emitter emitter,
            [Frozen]MethodDefinition method,
            IFixture fixture)
        {
            var cmd = Instruction.Create(OpCodes.Call, method);
            emitter.Body.Instructions.Add(cmd);
            fixture.Inject(cmd);

            fixture.Create<Processor>().RemoveStackArgument();

            Assert.True(emitter.Body.Instructions.Ordered(
                Cil.Cmd(OpCodes.Pop),
                Cil.Cmd(cmd.OpCode, cmd.Operand)
            ));
        }

        [Theory, AutoMoqData]
        internal void PushMethodArguments_Fields_InjectedArgumentsPushing(
            [Frozen(Matching.ImplementedInterfaces)]Emitter emitter,
            [Frozen]Instruction cmd,
            VariableDefinition var1, VariableDefinition var2,
            Processor proc)
        {
            emitter.Body.Instructions.Add(cmd);

            proc.PushMethodArguments(new[] {var1, var2});

            Assert.True(emitter.Body.Instructions.Ordered(
                Cil.Cmd(OpCodes.Ldloc, var1),
                Cil.Cmd(OpCodes.Ldloc, var2),
                Cil.Cmd(cmd.OpCode, cmd.Operand)
                ));
        }

        [Theory, AutoMoqData]
        internal void RemoveInstruction_ValidInput_InstructionRemoved(
            [Frozen(Matching.ImplementedInterfaces)]Emitter emitter,
            [Frozen]Instruction cmd,
            Processor proc)
        {
            emitter.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
            emitter.Body.Instructions.Add(cmd);
            emitter.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
            emitter.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

            proc.RemoveInstruction(cmd);

            Assert.DoesNotContain(emitter.Body.Instructions, i => i.Equals(cmd));
        }

        [Theory, AutoMoqData]
        internal void ReplaceToRetValueField_ValidInput_InstructionReplaced(
            [Frozen(Matching.ImplementedInterfaces)]Emitter emitter,
            [Frozen]Instruction cmd,
            FieldDefinition field,
            Processor proc)
        {
            emitter.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
            emitter.Body.Instructions.Add(cmd);
            emitter.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
            emitter.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

            proc.ReplaceToRetValueField(field);

            var replacedCmd = emitter.Body.Instructions[1];
            Assert.DoesNotContain(emitter.Body.Instructions, i => i.Equals(cmd));
            Assert.Equal(OpCodes.Ldsfld, replacedCmd.OpCode);
            Assert.Equal(field, replacedCmd.Operand);
        }
    }
}
