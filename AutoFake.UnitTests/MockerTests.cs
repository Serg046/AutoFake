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
        public void SaveMethodCall_MethodWithTwoArgs_ReturnTwoVariables()
        {
            var method = _typeInfo.Methods.Single(m => m.Name == nameof(TestType.SomeMethodWithBody));
            var proc = method.Body.GetILProcessor();
            var cmd = proc.Body.Instructions[8];
            var mocker = GetMocker(GetMock());

            var fields = mocker.SaveMethodCall(proc, cmd);

            Assert.Equal(2, fields.Count);
        }

        [Fact]
        public void RemoveMethodArguments_ValidInput_InjectedArgumentsRemoving()
        {
            var method = _typeInfo.Methods.Single(m => m.Name == nameof(TestType.SomeMethodWithBody));
            var proc = method.Body.GetILProcessor();
            var cmd = proc.Body.Instructions[8];
            var mocker = GetMocker(GetMock());

            mocker.RemoveMethodArguments(proc, cmd);

            Assert.True(proc.Body.Instructions.Ordered(
                Cil.Cmd(OpCodes.Pop),
                Cil.Cmd(OpCodes.Pop),
                Cil.Cmd(cmd.OpCode, cmd.Operand)
                ));
        }

        [Fact]
        public void RemoveStackArgument_ValidInput_InjectedStackArgumentRemoving()
        {
            var method = _typeInfo.Methods.Single(m => m.Name == nameof(TestType.SomeMethodWithBody));
            var proc = method.Body.GetILProcessor();
            var cmd = proc.Body.Instructions[8];
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
            var cmd = proc.Body.Instructions[8];
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
            var cmd = proc.Body.Instructions[8];
            var mocker = GetMocker(GetMock());

            mocker.RemoveInstruction(proc, cmd);

            Assert.DoesNotContain(proc.Body.Instructions, i => i.Equals(cmd));
        }

        [Fact]
        public void ReplaceToRetValueField_ValidInput_InstructionReplaced()
        {
            const int methodIndex = 8;
            var method = _typeInfo.Methods.Single(m => m.Name == nameof(TestType.SomeMethodWithBody));
            var proc = method.Body.GetILProcessor();
            var cmd = proc.Body.Instructions[methodIndex];
            var mocker = GetMocker(GetMock());
            mocker.GenerateRetValueField();

            mocker.ReplaceToRetValueField(proc, cmd);

            var replacedCmd = proc.Body.Instructions[methodIndex];

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
            var cmd = proc.Body.Instructions[8];
            var mocker = GetMocker(GetMock());
            mocker.GenerateCallbackField();

            mocker.InjectCallback(proc, cmd);

            Assert.True(proc.Body.Instructions.Ordered(
                Cil.Cmd(cmd.OpCode, cmd.Operand),
                Cil.Cmd(OpCodes.Ldsfld, mocker.MemberInfo.CallbackField),
                Cil.Cmd(OpCodes.Callvirt, (MethodReference m) => m.Name == "Invoke" && m.DeclaringType.Name == "Action")
                ));
        }

        private Mock GetMock() => new ReplaceableMock(Moq.Mock.Of<IInvocationExpression>(
            e => e.GetSourceMember() == GetSourceMember(nameof(TestType.SomeMethod))),
            new ReplaceableMock.Parameters());

        private Mocker GetMocker(Mock mock) => GetMocker(_typeInfo, mock);
        private Mocker GetMocker(TypeInfo typeInfo, Mock mock)
            => new Mocker(typeInfo, new MockedMemberInfo(mock, null, MOCKER_MEMBER_SUFFIX_NAME));

        private ISourceMember GetSourceMember(string name) => GetSourceMember<TestType>(name);
        private ISourceMember GetSourceMember<T>(string name) => new SourceMethod(typeof(T).GetMethod(name));

        private class TestType
        {
            public int SomeMethod() => 0;

            public void SomeMethodWithBody()
            {
                var a = 5;
                var b = a;
                SomeMethodWithArguments(a, b);
                var c = a + b;
            }

            public void SomeMethodWithArguments(int a, int b)
            {
            }
        }
    }
}
