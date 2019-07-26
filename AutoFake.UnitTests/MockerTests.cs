using System;
using System.Collections.Generic;
using System.Linq;
using AutoFake.Expression;
using AutoFake.Setup;
using AutoFake.UnitTests.TestUtils;
using Mono.Cecil.Cil;
using Xunit;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using Mono.Cecil;

namespace AutoFake.UnitTests
{
    public class MockerTests
    {
        private const string MOCKER_MEMBER_SUFFIX_NAME = "suffix";

        private readonly TypeInfo _typeInfo;

        public MockerTests()
        {
            _typeInfo = new TypeInfo(typeof(TestType), new List<FakeDependency>());
        }

        [Fact]
        public void GenerateSetupBodyField_FieldName_Added()
        {
            var mocker = GetMocker(GetMock());

            mocker.GenerateSetupBodyField();

            var expectedFieldName = $"SystemInt32_SomeMethod_{MOCKER_MEMBER_SUFFIX_NAME}_SetupBody";
            Assert.Equal(expectedFieldName, mocker.MemberInfo.SetupBodyField.Name);
            Assert.True(mocker.MemberInfo.SetupBodyField.Attributes.HasFlag(FieldAttributes.Assembly));
            Assert.True(mocker.MemberInfo.SetupBodyField.Attributes.HasFlag(FieldAttributes.Static));
            Assert.Contains(_typeInfo.Fields, f => f.Name == expectedFieldName);
        }

        [Fact]
        public void GenerateRetValueField_FieldName_Added()
        {
            var mocker = GetMocker(GetMock());

            mocker.GenerateRetValueField();

            var expectedFieldName = $"SystemInt32_SomeMethod_{MOCKER_MEMBER_SUFFIX_NAME}_RetValue";
            Assert.Equal(expectedFieldName, mocker.MemberInfo.RetValueField.Name);
            Assert.True(mocker.MemberInfo.RetValueField.Attributes.HasFlag(FieldAttributes.Assembly));
            Assert.True(mocker.MemberInfo.RetValueField.Attributes.HasFlag(FieldAttributes.Static));
            Assert.Contains(_typeInfo.Fields, f => f.Name == expectedFieldName);
        }

        [Fact]
        public void GenerateCallsCounterFuncField_FieldName_CounterFieldAdded()
        {
            var mocker = GetMocker(GetMock());

            mocker.GenerateCallsCounterFuncField();

            var expectedFieldName = $"SystemInt32_SomeMethod_{MOCKER_MEMBER_SUFFIX_NAME}_ExpectedCallsFunc";
            Assert.Equal(expectedFieldName, mocker.MemberInfo.ExpectedCallsFuncField.Name);
            Assert.True(mocker.MemberInfo.ExpectedCallsFuncField.Attributes.HasFlag(FieldAttributes.Assembly));
            Assert.True(mocker.MemberInfo.ExpectedCallsFuncField.Attributes.HasFlag(FieldAttributes.Static));
            Assert.Contains(_typeInfo.Fields, f => f.Name == expectedFieldName);
        }

        [Fact]
        public void SaveMethodCall_NoActualCallsAccumulator_Created()
        {
            var method = _typeInfo.Methods.Single(m => m.Name == nameof(TestType.SomeMethodWithBody));
            var proc = method.Body.GetILProcessor();
            var cmd = FindMethodCall(proc.Body);
            var mocker = GetMocker(GetMock());

            mocker.SaveMethodCall(proc, cmd);

            Assert.NotNull(mocker.MemberInfo.ActualCallsAccumulator?.VariableType);
            Assert.Equal(_typeInfo.Module.Import(typeof(List<object[]>)).FullName,
                mocker.MemberInfo.ActualCallsAccumulator.VariableType.FullName);
        }

        [Fact]
        public void SaveMethodCall_ActualCallsAccumulator_Reused()
        {
            var method = _typeInfo.Methods.Single(m => m.Name == nameof(TestType.SomeMethodWithBody));
            var proc = method.Body.GetILProcessor();
            var cmd = FindMethodCall(proc.Body);
            var mocker = GetMocker(GetMock());
            var expectedAccumulator = new VariableDefinition(new FunctionPointerType());
            mocker.MemberInfo.ActualCallsAccumulator = expectedAccumulator;

            mocker.SaveMethodCall(proc, cmd);

            Assert.Equal(expectedAccumulator, mocker.MemberInfo.ActualCallsAccumulator);
        }

        [Fact]
        public void SaveMethodCall_MethodWithTwoArgs_ReturnsTwoVariables()
        {
            var method = _typeInfo.Methods.Single(m => m.Name == nameof(TestType.SomeMethodWithBody));
            var proc = method.Body.GetILProcessor();
            var cmd = FindMethodCall(proc.Body);
            var mocker = GetMocker(GetMock());

            var variables = mocker.SaveMethodCall(proc, cmd);

            Assert.Equal(2, variables.Count);
        }

        [Fact]
        public void SaveMethodCall_CheckArgs_ArgsSaved()
        {
            var method = _typeInfo.Methods.Single(m => m.Name == nameof(TestType.SomeMethodWithBody));
            var proc = method.Body.GetILProcessor();
            var cmd = FindMethodCall(proc.Body);
            var mocker = GetMocker(GetMock(checkArguemnts: true));

            var variables = mocker.SaveMethodCall(proc, cmd);

            var arrVar = method.Body.Variables.Single(v => v.VariableType.FullName == "System.Object[]");
            Assert.True(proc.Body.Instructions.Ordered(
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
                Cil.Cmd(cmd.OpCode, cmd.Operand)
            ));
        }

        [Fact]
        public void RemoveMethodArgumentsIfAny_ValidInput_InjectedArgumentsRemoving()
        {
            var method = _typeInfo.Methods.Single(m => m.Name == nameof(TestType.SomeMethodWithBody));
            var proc = method.Body.GetILProcessor();
            var cmd = FindMethodCall(proc.Body);
            var mocker = GetMocker(GetMock());

            mocker.RemoveMethodArgumentsIfAny(proc, cmd);

            Assert.True(proc.Body.Instructions.Ordered(
                Cil.Cmd(OpCodes.Pop),
                Cil.Cmd(OpCodes.Pop),
                Cil.Cmd(cmd.OpCode, cmd.Operand)
                ));
        }

        [Fact]
        public void RemoveMethodArgumentsIfAny_Field_NothingInjected()
        {
            var method = _typeInfo.Methods.Single(m => m.Name == nameof(TestType.SomeMethodWithBody));
            var proc = method.Body.GetILProcessor();
            var cmd = FindFieldCall(proc.Body);
            var mocker = GetMocker(GetMock());
            var originalInstructions = proc.Body.Instructions.ToList();

            mocker.RemoveMethodArgumentsIfAny(proc, cmd);

            Assert.Equal(originalInstructions, proc.Body.Instructions);
        }

        [Fact]
        public void RemoveStackArgument_ValidInput_InjectedStackArgumentRemoving()
        {
            var method = _typeInfo.Methods.Single(m => m.Name == nameof(TestType.SomeMethodWithBody));
            var proc = method.Body.GetILProcessor();
            var cmd = FindMethodCall(proc.Body);
            var mocker = GetMocker(GetMock());

            mocker.RemoveStackArgument(proc, cmd);

            Assert.True(proc.Body.Instructions.Ordered(
                Cil.Cmd(OpCodes.Pop),
                Cil.Cmd(cmd.OpCode, cmd.Operand)
                ));
        }

        [Fact]
        public void PushMethodArguments_Fields_InjectedArgumentsPushing()
        {
            var variables = new List<VariableDefinition>();
            variables.Add(new VariableDefinition("Test0", _typeInfo.Module.Import(typeof(int))));
            variables.Add(new VariableDefinition("Test1", _typeInfo.Module.Import(typeof(int))));

            var method = _typeInfo.Methods.Single(m => m.Name == nameof(TestType.SomeMethodWithBody));
            var proc = method.Body.GetILProcessor();
            var cmd = FindMethodCall(proc.Body);
            var mocker = GetMocker(GetMock());

            mocker.PushMethodArguments(proc, cmd, variables);

            Assert.True(proc.Body.Instructions.Ordered(
                Cil.Cmd(OpCodes.Ldloc, variables[0]),
                Cil.Cmd(OpCodes.Ldloc, variables[1]),
                Cil.Cmd(cmd.OpCode, cmd.Operand)
                ));
        }

        [Fact]
        public void MemberInfo_ReturnsCorrectMemberInfo()
        {
            var mocker = GetMocker(GetMock());

            Assert.Equal(mocker.MemberInfo, ((IMethodMocker)mocker).MemberInfo);
        }

        [Fact]
        public void RemoveInstruction_ValidInput_InstructionRemoved()
        {
            var method = _typeInfo.Methods.Single(m => m.Name == nameof(TestType.SomeMethodWithBody));
            var proc = method.Body.GetILProcessor();
            var cmd = FindMethodCall(proc.Body);
            var mocker = GetMocker(GetMock());

            mocker.RemoveInstruction(proc, cmd);

            Assert.DoesNotContain(proc.Body.Instructions, i => i.Equals(cmd));
        }

        [Fact]
        public void ReplaceToRetValueField_ValidInput_InstructionReplaced()
        {
            var method = _typeInfo.Methods.Single(m => m.Name == nameof(TestType.SomeMethodWithBody));
            var proc = method.Body.GetILProcessor();
            var cmd = FindMethodCall(proc.Body, out var cmdIndex);
            var mocker = GetMocker(GetMock());
            mocker.GenerateRetValueField();

            mocker.ReplaceToRetValueField(proc, cmd);

            var replacedCmd = proc.Body.Instructions[cmdIndex];

            Assert.DoesNotContain(proc.Body.Instructions, i => i.Equals(cmd));
            Assert.Equal(OpCodes.Ldsfld, replacedCmd.OpCode);
            Assert.Equal(mocker.MemberInfo.RetValueField, replacedCmd.Operand);
        }

        [Fact]
        public void GenerateCallbackField_FieldName_CallbackFieldAdded()
        {
            var mocker = GetMocker(GetMock());

            mocker.GenerateCallbackField();

            var expectedFieldName = $"SystemInt32_SomeMethod_{MOCKER_MEMBER_SUFFIX_NAME}_Callback";
            Assert.Equal(expectedFieldName, mocker.MemberInfo.CallbackField.Name);
            Assert.True(mocker.MemberInfo.CallbackField.Attributes.HasFlag(FieldAttributes.Assembly));
            Assert.True(mocker.MemberInfo.CallbackField.Attributes.HasFlag(FieldAttributes.Static));
            Assert.Contains(_typeInfo.Fields, f => f.Name == expectedFieldName);
        }

        [Fact]
        public void InjectCallback_ValidInput_InjectedAfterInstruction()
        {
            var method = _typeInfo.Methods.Single(m => m.Name == nameof(TestType.SomeMethodWithBody));
            var proc = method.Body.GetILProcessor();
            var cmd = FindMethodCall(proc.Body);
            var mocker = GetMocker(GetMock());
            mocker.GenerateCallbackField();

            mocker.InjectCallback(proc, cmd);

            Assert.True(proc.Body.Instructions.Ordered(
                Cil.Cmd(cmd.OpCode, cmd.Operand),
                Cil.Cmd(OpCodes.Ldsfld, mocker.MemberInfo.CallbackField),
                Cil.Cmd(OpCodes.Callvirt, (MethodReference m) => m.Name == "Invoke" && m.DeclaringType.Name == "Action")
                ));
        }

        [Theory]
        [InlineData(false, false, false)]
        [InlineData(true, false, true)]
        [InlineData(false, true, true)]
        [InlineData(true, true, true)]
        public void InjectVerification_CheckArgsOrCallsCounter_Injected(bool checkArguments, bool callsCounter, bool injected)
        {
            var method = _typeInfo.Methods.Single(m => m.Name == nameof(TestType.SomeMethodWithBody));
            var proc = method.Body.GetILProcessor();
            var mocker = GetMocker(GetMock(checkArguments, callsCounter));
            mocker.MemberInfo.SetupBodyField = new FieldDefinition("testBody", FieldAttributes.Assembly, new FunctionPointerType());
            mocker.MemberInfo.ExpectedCallsFuncField = new FieldDefinition("testCounter", FieldAttributes.Assembly, new FunctionPointerType());
            mocker.MemberInfo.ActualCallsAccumulator = new VariableDefinition(new FunctionPointerType());

            mocker.InjectVerification(proc);

            var checkArgsCode = checkArguments ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0;
            var callsCounterCil = callsCounter
                ? Cil.Cmd(OpCodes.Ldsfld, mocker.MemberInfo.ExpectedCallsFuncField)
                : Cil.Cmd(OpCodes.Ldnull);
            Assert.True(proc.Body.Instructions.Ordered(
                Cil.Cmd(OpCodes.Ldsfld, mocker.MemberInfo.SetupBodyField),
                Cil.Cmd(OpCodes.Ldloc, mocker.MemberInfo.ActualCallsAccumulator),
                Cil.Cmd(checkArgsCode),
                callsCounterCil,
                Cil.Cmd(OpCodes.Callvirt, (MethodReference m) => m.Name == nameof(InvocationExpression.MatchArguments)
                    && m.DeclaringType.Name == nameof(InvocationExpression)),
                Cil.Cmd(OpCodes.Ret)
            ));
        }

        private Mock GetMock(bool checkArguemnts = false, bool callsCounter = false) => new ReplaceMock(Moq.Mock.Of<IInvocationExpression>(
            e => e.GetSourceMember() == GetSourceMember(nameof(TestType.SomeMethod))),
            new ReplaceMock.Parameters
            {
                CheckArguments = checkArguemnts,
                ExpectedCallsFunc = callsCounter ? (b => true) : (Func<byte, bool>)null 
            });

        private Mocker GetMocker(Mock mock) => GetMocker(_typeInfo, mock);
        private Mocker GetMocker(TypeInfo typeInfo, Mock mock)
            => new Mocker(typeInfo, new MockedMemberInfo(mock, MOCKER_MEMBER_SUFFIX_NAME));

        private ISourceMember GetSourceMember(string name) => GetSourceMember<TestType>(name);
        private ISourceMember GetSourceMember<T>(string name) => new SourceMethod(typeof(T).GetMethod(name));

        private Instruction FindMethodCall(MethodBody method) => FindMethodCall(method, out var _);
        private Instruction FindMethodCall(MethodBody method, out int index)
        {
            for (var i = 0; i < method.Instructions.Count; i++)
            {
                var instruction = method.Instructions[i];
                if (instruction.OpCode == OpCodes.Call && instruction.Operand is MethodDefinition m &&
                    m.Name == nameof(TestType.SomeMethodWithArguments))
                {
                    index = i;
                    return instruction;
                }
            }
            throw new InvalidOperationException("The method is not found");
        }

        private Instruction FindFieldCall(MethodBody method) => FindFieldCall(method, out var _);
        private Instruction FindFieldCall(MethodBody method, out int index)
        {
            for (var i = 0; i < method.Instructions.Count; i++)
            {
                var instruction = method.Instructions[i];
                if (instruction.OpCode == OpCodes.Ldfld && instruction.Operand is FieldDefinition f &&
                    f.Name == nameof(TestType.SomeField))
                {
                    index = i;
                    return instruction;
                }
            }
            throw new InvalidOperationException("The field is not found");
        }

        private class TestType
        {
            public int SomeField = 1;

            public int SomeMethod() => 0;

            public void SomeMethodWithBody()
            {
                var a = 5;
                var b = "a";
                SomeMethodWithArguments(a, b);
                var c = SomeField + a;
            }

            public void SomeMethodWithArguments(int a, string b)
            {
            }
        }
    }
}
