using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
            var field = proc.GenerateField(propName, propType);

            Assert.Equal(propName, field.Name);
            Assert.True(field.Attributes.HasFlag(FieldAttributes.Assembly));
            Assert.True(field.Attributes.HasFlag(FieldAttributes.Static));
            Assert.Equal(propType.FullName, field.FieldType.FullName);
            typeInfo.Verify(t => t.AddField(field));
        }

        [Theory, AutoMoqData]
        internal void GenerateRetValueField_TypeExists_Reused(
            [Frozen]ModuleDefinition module,
            [Frozen]Mock<ITypeInfo> typeInfo,
            string fieldName, Type fieldType,
            PrePostProcessor proc)
        {
            var typeDef = new TypeDefinition(fieldType.Namespace, fieldType.Name, TypeAttributes.Class);
            module.Types.Add(typeDef);
            var field = proc.GenerateField(fieldName, fieldType);

            Assert.Equal(fieldName, field.Name);
            Assert.True(field.Attributes.HasFlag(FieldAttributes.Assembly));
            Assert.True(field.Attributes.HasFlag(FieldAttributes.Static));
            Assert.Equal(fieldType.FullName, field.FieldType.FullName);
            typeInfo.Verify(t => t.AddField(field));
        }

        [Theory, AutoMoqData]
        internal void GenerateCallsCounterFuncField_FieldName_CounterFieldAdded(
            [Frozen]ModuleDefinition module,
            MethodBody method,
            string fieldName,
            PrePostProcessor proc)
        {
            var accumulator = proc.GenerateCallsAccumulator(fieldName, method);

            var type = module.Import(typeof(List<object[]>));
            Assert.Equal(type.FullName, accumulator.FieldType.FullName);
            Assert.True(method.Instructions.Ordered(
                Cil.Cmd(OpCodes.Newobj, (MethodReference m) => m.Name == ".ctor"
                                                               && m.DeclaringType.FullName == type.FullName),
                Cil.Cmd(OpCodes.Stsfld, accumulator)
            ));
        }

        [Theory]
        [InlineAutoMoqData(false, false)]
        [InlineAutoMoqData(true, false)]
        [InlineAutoMoqData(false, true)]
        [InlineAutoMoqData(true, true)]
        internal void InjectVerification_CheckCalls_Injected(
            bool checkArguments, bool callsCounter,
            [Frozen]ModuleDefinition module,
            [Frozen]Emitter emitter,
            TypeDefinition type, MethodDefinition ctor, MethodDefinition method,
            FieldDefinition setupBody, FieldDefinition accumulator,
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
                Cil.Cmd(OpCodes.Ldsfld, accumulator),
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

        [Theory]
        [InlineAutoMoqData(typeof(Task))]
        [InlineAutoMoqData(typeof(Task<int>))]
        internal void InjectVerification_AsyncMethod_Injected(
            Type asyncType,
            [Frozen]ModuleDefinition module,
            [Frozen]Emitter emitter,
            TypeDefinition type, MethodDefinition ctor, MethodDefinition method,
            FieldDefinition setupBody, FieldDefinition accumulator,
            PrePostProcessor proc)
        {
            var taskType = module.ImportReference(asyncType);
            emitter.Body.Method.ReturnType = taskType;
            emitter.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            ctor.Name = ".ctor";
            type.Methods.Add(ctor);
            type.Methods.Add(method);
            module.Types.Add(type);
            var callsCounterDescriptor = new MethodDescriptor(type.FullName, method.Name);

            proc.InjectVerification(emitter, true, callsCounterDescriptor, setupBody, accumulator);

            Assert.True(emitter.Body.Instructions.Ordered(
                Cil.Cmd(OpCodes.Stloc, (VariableDefinition o) => o.VariableType == taskType),
                Cil.Cmd(OpCodes.Ldsfld, setupBody),
                Cil.Cmd(OpCodes.Ldloc, (VariableDefinition o) => o.VariableType == taskType),
                Cil.Cmd(OpCodes.Ldsfld, accumulator),
                Cil.Cmd(OpCodes.Ldc_I4_1),
                Cil.Cmd(OpCodes.Newobj, ctor),
                Cil.Cmd(OpCodes.Ldftn, method),
                Cil.Cmd(OpCodes.Newobj, (MemberReference m) => m.Name == ".ctor"
                    && m.DeclaringType.FullName == "System.Func`2<System.Byte,System.Boolean>"),
                Cil.Cmd(OpCodes.Callvirt, (MethodReference m) =>
                    m.Name == nameof(InvocationExpression.MatchArgumentsAsync)
                    && m.DeclaringType.Name == nameof(InvocationExpression))
            ));
        }
    }
}
