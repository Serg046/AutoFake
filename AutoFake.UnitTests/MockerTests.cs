using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

        private ISourceMember GetSourceMember(string name) => GetSourceMember<TestType>(name);
        private ISourceMember GetSourceMember<T>(string name) => new SourceMethod(typeof(T).GetMethod(name));

        private readonly TypeInfo _typeInfo;

        public MockerTests()
        {
            _typeInfo = new TypeInfo(typeof(TestType), new List<FakeDependency>());
        }
        
        private Mock GetMock(string methodName) => GetMock(methodName, new List<FakeArgument>());
        private Mock GetMock(string methodName, List<FakeArgument> arguments) => new ReplaceableMock(GetSourceMember(methodName), arguments, new ReplaceableMock.Parameters());

        private Mocker GetMocker(Mock mock) => GetMocker(_typeInfo, mock);
        private Mocker GetMocker(TypeInfo typeInfo, Mock mock)
            => new Mocker(typeInfo, new MockedMemberInfo(mock, null, MOCKER_MEMBER_SUFFIX_NAME));

        private ILProcessor GetILProcessor() => new Mono.Cecil.Cil.MethodBody(null).GetILProcessor();
        
        [Fact]
        public void GenerateRetValueField_FieldName_Added()
        {
            var mocker = GetMocker(GetMock(nameof(TestType.SomeMethod)));

            mocker.GenerateRetValueField();

            var expectedFieldName = $"SystemInt32_SomeMethod_{MOCKER_MEMBER_SUFFIX_NAME}_RetValue";
            Assert.Equal(expectedFieldName, mocker.MemberInfo.RetValueField.Name);
            Assert.True(mocker.MemberInfo.RetValueField.Attributes.HasFlag(FieldAttributes.Assembly));
            Assert.True(mocker.MemberInfo.RetValueField.Attributes.HasFlag(FieldAttributes.Static));
            Assert.Contains(_typeInfo.Fields, f => f.Name == expectedFieldName);
        }

        [Fact]
        public void GenerateCallsCounter_FieldName_CounterFieldAdded()
        {
            var mocker = GetMocker(GetMock(nameof(TestType.SomeMethod)));

            mocker.GenerateCallsCounter();

            var expectedFieldName = $"SystemInt32_SomeMethod_{MOCKER_MEMBER_SUFFIX_NAME}_ActualIds";
            Assert.Equal(expectedFieldName, mocker.MemberInfo.ActualCallsField.Name);
            Assert.True(mocker.MemberInfo.ActualCallsField.Attributes.HasFlag(FieldAttributes.Assembly));
            Assert.True(mocker.MemberInfo.ActualCallsField.Attributes.HasFlag(FieldAttributes.Static));
            Assert.Contains(_typeInfo.Fields, f => f.Name == expectedFieldName);
        }

        [Fact]
        public void GenerateCallsCounter_FieldName_StaticCtorAddedOrUpdated()
        {
            var mocker = GetMocker(GetMock(nameof(TestType.SomeMethod)));

            mocker.GenerateCallsCounter();

            var field = _typeInfo.Fields.Single(f => f.Name == $"SystemInt32_SomeMethod_{MOCKER_MEMBER_SUFFIX_NAME}_ActualIds");
            var cctor = _typeInfo.Methods.Single(m => m.Name == ".cctor");

            Assert.Contains(cctor.Body.Instructions, i => i.OpCode == OpCodes.Stsfld && i.Operand == field);
        }

        [Fact]
        public void GenerateCallsCounter_DoubleInvoke_SingleCCtor()
        {
            var mocker = GetMocker(GetMock(nameof(TestType.SomeMethod)));

            mocker.GenerateCallsCounter();
            mocker.GenerateCallsCounter();

            _typeInfo.Methods.Single(m => m.Name == ".cctor");
        }

        [Fact]
        public void GenerateCallsCounter_FieldName_CounterFieldInitialized()
        {
            var typeInfo = new TypeInfo(typeof(TestTypeWithStaticConstructor), new List<FakeDependency>());
            var mocker = GetMocker(typeInfo, new ReplaceableMock(GetSourceMember(nameof(TestType.SomeMethod)), new List<FakeArgument>(), new ReplaceableMock.Parameters()));

            mocker.GenerateCallsCounter();

            var field = typeInfo.Fields.Single(f => f.Name == $"SystemInt32_SomeMethod_{MOCKER_MEMBER_SUFFIX_NAME}_ActualIds");
            var cctor = typeInfo.Methods.Single(m => m.Name == ".cctor");

            Assert.True(cctor.Body.Instructions.Ordered(
                Cil.Cmd(OpCodes.Newobj, (MethodReference m) => m.Name == ".ctor" && m.DeclaringType.Name == "List`1"),
                Cil.Cmd(OpCodes.Stsfld, field)));
        }

        [Fact]
        public void InjectCurrentPositionSaving_ValidInput_InjectedAfterInstruction()
        {
            var method = _typeInfo.Methods.Single(m => m.Name == nameof(TestType.SomeMethodWithBody));
            var proc = method.Body.GetILProcessor();
            var cmd = proc.Body.Instructions[1];
            var mocker = GetMocker(GetMock(nameof(TestType.SomeMethod)));

            mocker.MemberInfo.ActualCallsField = new FieldDefinition("Test", FieldAttributes.Private, _typeInfo.Import(typeof(int)));

            mocker.InjectCurrentPositionSaving(proc, cmd);

            Assert.True(proc.Body.Instructions.Ordered(
                Cil.Cmd(cmd.OpCode, cmd.Operand),
                Cil.Cmd(OpCodes.Ldsfld, mocker.MemberInfo.ActualCallsField),
                Cil.Cmd(OpCodes.Ldc_I4, mocker.MemberInfo.SourceCodeCallsCount),
                Cil.Cmd(OpCodes.Callvirt, (MethodReference m) => m.Name == "Add" && m.DeclaringType.Name == "List`1"),
                Cil.AnyCmd() //last instruction, see SomeType::SomeMethodWithBody()
                ));
        }

        [Fact]
        public void PopMethodArguments_MethodWithTwoArgs_ReturnNewTwoFields()
        {
            var method = _typeInfo.Methods.Single(m => m.Name == nameof(TestType.SomeMethodWithBody));
            var proc = method.Body.GetILProcessor();
            var cmd = proc.Body.Instructions[1];
            var mocker = GetMocker(GetMock(nameof(TestType.SomeMethodWithArguments), GetSetupArguments()));

            var fields = mocker.PopMethodArguments(proc, cmd);

            Assert.Equal(2, fields.OfType<FieldDefinition>().Count());
        }

        [Fact]
        public void PopMethodArguments_MethodWithTwoArgs_CorrectAccessFieldModificators()
        {
            var method = _typeInfo.Methods.Single(m => m.Name == nameof(TestType.SomeMethodWithBody));
            var proc = method.Body.GetILProcessor();
            var cmd = proc.Body.Instructions[1];
            var mocker = GetMocker(GetMock(nameof(TestType.SomeMethodWithArguments), GetSetupArguments()));

            var fields = mocker.PopMethodArguments(proc, cmd);

            foreach (var fieldDefinition in fields)
            {
                Assert.True(fieldDefinition.Attributes.HasFlag(FieldAttributes.Assembly));
                Assert.True(fieldDefinition.Attributes.HasFlag(FieldAttributes.Static));
            }
        }

        [Theory]
        [InlineData(0, "SomeMethodWithArgumentsArgument0", "SomeMethodWithArgumentsArgument1")]
        [InlineData(1, "SomeMethodWithArgumentsArgument2", "SomeMethodWithArgumentsArgument3")]
        [InlineData(10, "SomeMethodWithArgumentsArgument20", "SomeMethodWithArgumentsArgument21")]
        public void PopMethodArguments_ValidInput_CorrectFieldName(int sourceCallsCount, string firstArg, string secondArg)
        {
            var method = _typeInfo.Methods.Single(m => m.Name == nameof(TestType.SomeMethodWithBody));
            var proc = method.Body.GetILProcessor();
            var cmd = proc.Body.Instructions[1];
            var mocker = GetMocker(GetMock(nameof(TestType.SomeMethodWithArguments), GetSetupArguments()));
            mocker.MemberInfo.SourceCodeCallsCount = sourceCallsCount;

            var fields = mocker.PopMethodArguments(proc, cmd);

            Assert.Equal(firstArg, fields[0].Name);
            Assert.Equal(secondArg, fields[1].Name);
        }

        [Fact]
        public void PopMethodArguments_ValidInput_InjectedFieldInitialization()
        {
            var method = _typeInfo.Methods.Single(m => m.Name == nameof(TestType.SomeMethodWithBody));
            var proc = method.Body.GetILProcessor();
            var cmd = proc.Body.Instructions[1];
            var mocker = GetMocker(GetMock(nameof(TestType.SomeMethodWithArguments), GetSetupArguments()));

            var fields = mocker.PopMethodArguments(proc, cmd);

            Assert.True(proc.Body.Instructions.Ordered(
                Cil.Cmd(OpCodes.Stsfld, fields[1]),
                Cil.Cmd(OpCodes.Stsfld, fields[0]),
                Cil.Cmd(cmd.OpCode, cmd.Operand)
                ));
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData(1, null)]
        [InlineData(null, 1)]
        public void PopMethodArguments_NullAsInstalledArg_Success(object arg1, object arg2)
        {
            var method = _typeInfo.Methods.Single(m => m.Name == nameof(TestType.SomeMethodWithTwoObjectArguments));
            var proc = method.Body.GetILProcessor();
            var cmd = proc.Body.Instructions[0];
            var arguments = new List<FakeArgument>{ GetFakeArgument(arg1), GetFakeArgument(arg2) };
            var mocker = GetMocker(GetMock(nameof(TestType.SomeMethodWithTwoObjectArguments), arguments));

            mocker.PopMethodArguments(proc, cmd);
        }

        [Fact]
        public void RemoveMethodArguments_ValidInput_InjectedArgumentsRemoving()
        {
            var method = _typeInfo.Methods.Single(m => m.Name == nameof(TestType.SomeMethodWithBody));
            var proc = method.Body.GetILProcessor();
            var cmd = proc.Body.Instructions[1];
            var mocker = GetMocker(GetMock(nameof(TestType.SomeMethodWithArguments), GetSetupArguments()));

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
            var cmd = proc.Body.Instructions[1];
            var mocker = GetMocker(GetMock(nameof(TestType.SomeMethod)));

            mocker.RemoveStackArgument(proc, cmd);

            Assert.True(proc.Body.Instructions.Ordered(
                Cil.Cmd(OpCodes.Pop),
                Cil.Cmd(cmd.OpCode, cmd.Operand)
                ));
        }

        [Fact]
        public void PushMethodArguments_Fields_InjectedArgumentsPushing()
        {
            var fields = new List<FieldDefinition>();
            fields.Add(new FieldDefinition("Test0", FieldAttributes.Private, _typeInfo.Import(typeof(int))));
            fields.Add(new FieldDefinition("Test1", FieldAttributes.Private, _typeInfo.Import(typeof(int))));

            var method = _typeInfo.Methods.Single(m => m.Name == nameof(TestType.SomeMethodWithBody));
            var proc = method.Body.GetILProcessor();
            var cmd = proc.Body.Instructions[1];
            var mocker = GetMocker(GetMock(nameof(TestType.SomeMethod)));

            mocker.PushMethodArguments(proc, cmd, fields);

            Assert.True(proc.Body.Instructions.Ordered(
                Cil.Cmd(OpCodes.Ldsfld, fields[0]),
                Cil.Cmd(OpCodes.Ldsfld, fields[1]),
                Cil.Cmd(cmd.OpCode, cmd.Operand)
                ));
        }

        [Fact]
        public void MemberInfo_ReturnsCorrectMemberInfo()
        {
            var mocker = GetMocker(GetMock(nameof(TestType.SomeMethod)));

            Assert.Equal(mocker.MemberInfo, ((IMethodMocker)mocker).MemberInfo);
        }

        [Fact]
        public void RemoveInstruction_ValidInput_InstructionRemoved()
        {
            var method = _typeInfo.Methods.Single(m => m.Name == nameof(TestType.SomeMethodWithBody));
            var proc = method.Body.GetILProcessor();
            var cmd = proc.Body.Instructions[1];
            var mocker = GetMocker(GetMock(nameof(TestType.SomeMethod)));

            mocker.RemoveInstruction(proc, cmd);

            Assert.DoesNotContain(proc.Body.Instructions, i => i.Equals(cmd));
        }

        [Fact]
        public void ReplaceToRetValueField_ValidInput_InstructionReplaced()
        {
            var method = _typeInfo.Methods.Single(m => m.Name == nameof(TestType.SomeMethodWithBody));
            var proc = method.Body.GetILProcessor();
            var cmd = proc.Body.Instructions[1];
            var mocker = GetMocker(GetMock(nameof(TestType.SomeMethodWithBody)));
            mocker.GenerateRetValueField();

            mocker.ReplaceToRetValueField(proc, cmd);

            var replacedCmd = proc.Body.Instructions[1];

            Assert.DoesNotContain(proc.Body.Instructions, i => i.Equals(cmd));
            Assert.Equal(OpCodes.Ldsfld, replacedCmd.OpCode);
            Assert.Equal(mocker.MemberInfo.RetValueField, replacedCmd.Operand);
        }

        [Fact]
        public void GenerateCallbackField_FieldName_CallbackFieldAdded()
        {
            var mocker = GetMocker(GetMock(nameof(TestType.SomeMethod)));

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
            var cmd = proc.Body.Instructions[1];
            var mocker = GetMocker(GetMock(nameof(TestType.SomeMethod)));
            mocker.GenerateCallbackField();

            mocker.InjectCallback(proc, cmd);

            Assert.True(proc.Body.Instructions.Ordered(
                Cil.Cmd(cmd.OpCode, cmd.Operand),
                Cil.Cmd(OpCodes.Ldsfld, mocker.MemberInfo.CallbackField),
                Cil.Cmd(OpCodes.Callvirt, (MethodReference m) => m.Name == "Invoke" && m.DeclaringType.Name == "Action")
                ));
        }

        private static FakeArgument GetFakeArgument(dynamic value)
            => new FakeArgument(new EqualityArgumentChecker(value));

        private List<FakeArgument> GetSetupArguments()
        {
            return new List<FakeArgument> { GetFakeArgument(0), GetFakeArgument(0) };
        }

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

            public void SomeMethodWithTwoObjectArguments(object a, object b)
            {
            }
        }

        private class TestTypeWithStaticConstructor
        {
            static TestTypeWithStaticConstructor()
            {
                var a = 5;
                var b = a;
                Debug.WriteLine(a + b);
            }
        }
    }
}
