using System;
using System.Collections.Generic;
using System.Linq;
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
        internal void GenerateField_FieldName_Added(
            [Frozen, InjectModule] Mock<ITypeInfo> _,
            [Frozen]Mock<ITypeInfo> typeInfo,
            string propName, Type propType,
            PrePostProcessor proc)
        {
            var field = proc.GenerateField(propName, propType);

            Assert.Equal(propName, field.Name);
            Assert.True(field.Attributes.HasFlag(FieldAttributes.Public));
            Assert.True(field.Attributes.HasFlag(FieldAttributes.Static));
            Assert.Equal(propType.FullName, field.FieldType.FullName);
            typeInfo.Verify(t => t.AddField(field));
        }

        [Theory, AutoMoqData]
        internal void GenerateCallsCounterFuncField_FieldName_CounterFieldAdded(
            [Frozen]ModuleDefinition module,
            [Frozen, InjectModule] Mock<ITypeInfo> _,
            MethodBody method,
            string fieldName,
            PrePostProcessor proc)
        {
	        method.Method.DeclaringType = module.Types.First();
            var accumulator = proc.GenerateCallsAccumulator(fieldName, method);

            var type = module.ImportReference(typeof(List<object[]>));
            Assert.Equal(type.FullName, accumulator.FieldType.FullName);
            Assert.True(method.Instructions.Ordered(
                Cil.Cmd(OpCodes.Newobj, (MethodReference m) => m.Name == ".ctor"
                                                               && m.DeclaringType.FullName == type.FullName),
                Cil.Cmd(OpCodes.Stsfld, accumulator)
            ));
        }

        [Theory]
        [InlineAutoMoqData(typeof(Task), nameof(InvocationExpression.VerifyExpectedCallsAsync))]
        [InlineAutoMoqData(typeof(Task<int>), nameof(InvocationExpression.VerifyExpectedCallsTypedAsync))]
        internal void InjectVerification_AsyncMethod_Injected(
            Type asyncType, string checkerMethodName,
            [Frozen, InjectModule] Mock<ITypeInfo> _,
            [Frozen] ModuleDefinition module,
            [Frozen]Emitter emitter,
            FieldDefinition setupBody, FieldDefinition executionCtx,
            PrePostProcessor proc)
        {
            var taskType = module.ImportReference(asyncType);
            emitter.Body.Method.ReturnType = taskType;
            emitter.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

            proc.InjectVerification(emitter, setupBody, executionCtx);

            Assert.True(emitter.Body.Instructions.Ordered(
				Cil.Cmd(OpCodes.Stloc, (VariableDefinition o) => o.VariableType == taskType),
				Cil.Cmd(OpCodes.Ldsfld, setupBody),
				Cil.Cmd(OpCodes.Ldloc, (VariableDefinition o) => o.VariableType == taskType),
                Cil.Cmd(OpCodes.Ldsfld, executionCtx),
                Cil.Cmd(OpCodes.Call, (MethodReference m) =>
                    m.Name == checkerMethodName
                    && m.DeclaringType.Name == nameof(InvocationExpression))
            ));
        }
    }
}
