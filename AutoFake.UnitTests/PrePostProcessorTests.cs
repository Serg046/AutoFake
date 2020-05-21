﻿using System;
using System.Collections.Generic;
using AutoFake.Expression;
using AutoFake.UnitTests.TestUtils;
using AutoFixture.Xunit2;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Moq;
using Xunit;

namespace AutoFake.UnitTests
{
    public class PrePostProcessorTests
    {
        [Theory, AutoMoqData]
        internal void GenerateSetupBodyField_FieldName_Added(
            [Frozen]Mock<ITypeInfo> typeInfo,
            string propName,
            PrePostProcessor proc)
        {
            var field = proc.GenerateSetupBodyField(propName);

            Assert.Equal(propName, field.Name);
            Assert.True(field.Attributes.HasFlag(FieldAttributes.Assembly));
            Assert.True(field.Attributes.HasFlag(FieldAttributes.Static));
            Assert.Equal(typeof(InvocationExpression).FullName, field.FieldType.FullName);
            typeInfo.Verify(t => t.AddField(field));
        }

        [Theory, AutoMoqData]
        internal void GenerateRetValueField_FieldName_Added(
            [Frozen]Mock<ITypeInfo> typeInfo,
            string propName, Type propType,
            PrePostProcessor proc)
        {
            var field = proc.GenerateRetValueField(propName, propType);

            Assert.Equal(propName, field.Name);
            Assert.True(field.Attributes.HasFlag(FieldAttributes.Assembly));
            Assert.True(field.Attributes.HasFlag(FieldAttributes.Static));
            Assert.Equal(propType.FullName, field.FieldType.FullName);
            typeInfo.Verify(t => t.AddField(field));
        }

        [Theory, AutoMoqData]
        internal void GenerateCallsCounterFuncField_FieldName_CounterFieldAdded(
            [Frozen]ModuleDefinition module,
            MethodBody method,
            PrePostProcessor proc)
        {
            var accumulator = proc.GenerateCallsAccumulator(method);

            var type = module.Import(typeof(List<object[]>));
            Assert.Equal(type.FullName,
                accumulator.VariableType.FullName);
            Assert.Contains(accumulator, method.Variables);
            Assert.True(method.Instructions.Ordered(
                Cil.Cmd(OpCodes.Newobj, (MethodReference m) => m.Name == ".ctor"
                                                               && m.DeclaringType.FullName == type.FullName),
                Cil.Cmd(OpCodes.Stloc, accumulator)
            ));
        }

        [Theory]
        [InlineAutoMoqData(false, false, false)]
        [InlineAutoMoqData(true, false, true)]
        [InlineAutoMoqData(false, true, true)]
        [InlineAutoMoqData(true, true, true)]
        internal void InjectVerification_CheckCalls_Injected(
            bool checkArguments, bool callsCounter, bool injected,
            [Frozen]ModuleDefinition module,
            [Frozen]Emitter emitter,
            TypeDefinition type, MethodDefinition ctor, MethodDefinition method,
            FieldDefinition setupBody, VariableDefinition accumulator,
            PrePostProcessor proc)
        {
            emitter.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            ctor.Name = ".ctor";
            type.Methods.Add(ctor);
            type.Methods.Add(method);
            module.Types.Add(type);
            var callsCounterDescriptor = callsCounter ? new MethodDescriptor(type.FullName, method.Name) : null;

            proc.InjectVerification(emitter, checkArguments, callsCounterDescriptor, setupBody, accumulator);

            var checkArgsCode = checkArguments ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0;
            var instructions = new List<Cil>
            {
                Cil.Cmd(OpCodes.Ldsfld, setupBody),
                Cil.Cmd(OpCodes.Ldloc, accumulator),
                Cil.Cmd(checkArgsCode)
            };
            if (callsCounter)
            {
                instructions.Add(Cil.Cmd(OpCodes.Newobj, ctor));
                instructions.Add(Cil.Cmd(OpCodes.Ldftn, method));
                instructions.Add(Cil.Cmd(OpCodes.Newobj, (MemberReference m) => m.Name == ".ctor"
                    && m.DeclaringType.FullName == "System.Func`2<System.Byte,System.Boolean>"));
            }
            else
            {
                instructions.Add(Cil.Cmd(OpCodes.Ldnull));
            }
            instructions.Add(Cil.Cmd(OpCodes.Callvirt, (MethodReference m) =>
                m.Name == nameof(InvocationExpression.MatchArguments)
                && m.DeclaringType.Name == nameof(InvocationExpression)));
            instructions.Add(Cil.Cmd(OpCodes.Ret));
            Assert.True(emitter.Body.Instructions.Ordered(instructions));
        }
    }
}