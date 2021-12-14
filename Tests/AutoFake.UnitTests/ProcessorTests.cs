using System;
using System.Linq;
using AutoFake.Setup.Mocks;
using AutoFake.UnitTests.TestUtils;
using AutoFixture;
using AutoFixture.Xunit2;
using Mono.Cecil.Cil;
using Xunit;
using Mono.Cecil;
using Moq;

namespace AutoFake.UnitTests
{
    public class ProcessorTests
    {
        [Theory, AutoMoqData]
        internal void SaveMethodCall_MethodWithTwoArgs_ReturnsTwoVariables(
            [Frozen] ModuleDefinition module,
            MethodDefinition method,
            FieldDefinition setupBody, FieldDefinition executionContext,
            IFixture fixture)
        {
	        method.DeclaringType = module.Types.First();
            method.Parameters.Add(new ParameterDefinition(new FunctionPointerType()));
            method.Parameters.Add(new ParameterDefinition(new FunctionPointerType()));
            fixture.Inject(Instruction.Create(OpCodes.Call, method));

            var variables = fixture.Create<Processor>().RecordMethodCall(setupBody, executionContext,
	            new[] { typeof(object), typeof(object) }.ToReadOnlyList());

            Assert.Equal(2, variables.Count);
        }

        [Theory, AutoMoqData]
        internal void SaveMethodCall_CheckArgs_ArgsSaved(
            [Frozen, InjectModule] Mock<ITypeInfo> _,
            [Frozen(Matching.ImplementedInterfaces)]Emitter emitter,
            [Frozen] ModuleDefinition module,
            TypeDefinition refType,
            MethodDefinition method,
            FieldDefinition setupBody, FieldDefinition executionContext,
            IFixture fixture)
        {
	        method.DeclaringType = module.Types.First();
            method.Parameters.Add(new ParameterDefinition(TypeExtensions.ValueTypeDef()));
            method.Parameters.Add(new ParameterDefinition(refType));
            var instruction = Instruction.Create(OpCodes.Call, method);
            emitter.Body.Instructions.Add(instruction);
            fixture.Inject(instruction);

            var variables = fixture.Create<Processor>().RecordMethodCall(setupBody, executionContext,
	            new[] { typeof(int), typeof(object) }.ToReadOnlyList());

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
                Cil.AnyCmd(),
                Cil.AnyCmd(),
                Cil.Cmd(instruction.OpCode, instruction.Operand)
            ));
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
    }
}
