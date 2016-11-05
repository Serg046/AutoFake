using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using AutoFake.Setup;
using GuardExtensions;
using Mono.Cecil.Cil;
using Xunit;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using Mono.Cecil;

namespace AutoFake.UnitTests
{
    public class MockerTests
    {
        private class SomeType
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

        private class SomeTypeWithStaticConstructor
        {
            static SomeTypeWithStaticConstructor()
            {
                var a = 5;
                var b = a;
                Debug.WriteLine(a + b);
            }
        }

        private MethodInfo GetMethodInfo(string name) => GetMethodInfo<SomeType>(name);
        private MethodInfo GetMethodInfo<T>(string name) => typeof(T).GetMethod(name);

        private readonly TypeInfo _typeInfo;
        private readonly FakeSetupPack _setup;
        private readonly Mocker _mocker;

        public MockerTests()
        {
            _typeInfo = new TypeInfo(typeof(SomeType), new List<FakeDependency>());
            _setup = new FakeSetupPack();
            _mocker = new Mocker(_typeInfo, _setup);

            _typeInfo.Load();
        }

        private ILProcessor GetILProcessor() => new Mono.Cecil.Cil.MethodBody(null).GetILProcessor();

        [Fact]
        public void Ctor_Null_Throws()
        {
            Assert.Throws<ContractFailedException>(() => new Mocker(null, new FakeSetupPack()));
            Assert.Throws<ContractFailedException>(() => new Mocker(_typeInfo, null));
        }

        [Fact]
        public void GenerateRetValueField_InvalidInput_Throws()
        {
            var setup = new FakeSetupPack();
            setup.ReturnObjectFieldName = null;
            setup.IsVoid = false;
            Assert.Throws<ContractFailedException>(() => new Mocker(_typeInfo, setup).GenerateRetValueField());

            setup.ReturnObjectFieldName = string.Empty;
            setup.IsVoid = true;
            Assert.Throws<ContractFailedException>(() => new Mocker(_typeInfo, setup).GenerateRetValueField());
        }

        [Fact]
        public void GenerateRetValueField_FieldName_Added()
        {
            _setup.ReturnObjectFieldName = "Test";
            _setup.Method = GetMethodInfo(nameof(SomeType.SomeMethod));

            _mocker.GenerateRetValueField();

            Assert.Equal("Test_RetValue", _mocker.MemberInfo.RetValueField.Name);
            Assert.True(_mocker.MemberInfo.RetValueField.Attributes.HasFlag(FieldAttributes.Assembly));
            Assert.True(_mocker.MemberInfo.RetValueField.Attributes.HasFlag(FieldAttributes.Static));
            Assert.Contains(_typeInfo.Fields, f => f.Name == "Test_RetValue");
        }

        [Fact]
        public void GenerateCallsCounter_InvalidInput_Throws()
        {
            _setup.ReturnObjectFieldName = null;
            Assert.Throws<ContractFailedException>(() => _mocker.GenerateCallsCounter());
        }

        [Fact]
        public void GenerateCallsCounter_FieldName_CounterFieldAdded()
        {
            _setup.ReturnObjectFieldName = "TestCounter";

            _mocker.GenerateCallsCounter();

            Assert.Equal("TestCounter_ActualIds", _mocker.MemberInfo.ActualCallsField.Name);
            Assert.True(_mocker.MemberInfo.ActualCallsField.Attributes.HasFlag(FieldAttributes.Assembly));
            Assert.True(_mocker.MemberInfo.ActualCallsField.Attributes.HasFlag(FieldAttributes.Static));
            Assert.Contains(_typeInfo.Fields, f => f.Name == "TestCounter_ActualIds");
        }

        [Fact]
        public void GenerateCallsCounter_FieldName_StaticCtorAddedOrUpdated()
        {
            _setup.ReturnObjectFieldName = "TestCounter";

            _mocker.GenerateCallsCounter();

            var field = _typeInfo.Fields.Single(f => f.Name == "TestCounter_ActualIds");
            var cctor = _typeInfo.Methods.Single(m => m.Name == ".cctor");

            Assert.Contains(cctor.Body.Instructions, i => i.OpCode == OpCodes.Stsfld && i.Operand == field);
        }

        [Fact]
        public void GenerateCallsCounter_DoubleInvoke_SingleCCtor()
        {
            _setup.ReturnObjectFieldName = "TestCounter";

            _mocker.GenerateCallsCounter();
            _mocker.GenerateCallsCounter();

            _typeInfo.Methods.Single(m => m.Name == ".cctor");
        }

        [Fact]
        public void GenerateCallsCounter_FieldName_CounterFieldInitialized()
        {
            var typeInfo = new TypeInfo(typeof(SomeTypeWithStaticConstructor), new List<FakeDependency>());
            typeInfo.Load();
            var setup = new FakeSetupPack();
            setup.ReturnObjectFieldName = "TestCounter";
            var mocker = new Mocker(typeInfo, setup);

            mocker.GenerateCallsCounter();

            var field = typeInfo.Fields.Single(f => f.Name == "TestCounter_ActualIds");
            var cctor = typeInfo.Methods.Single(m => m.Name == ".cctor");

            Assert.True(cctor.Body.Instructions.Ordered(
                Cil.Cmd(OpCodes.Newobj, (MethodReference m) => m.Name == ".ctor" && m.DeclaringType.Name == "List`1"),
                Cil.Cmd(OpCodes.Stsfld, field)));
        }

        [Fact]
        public void InjectCurrentPositionSaving_Null_Throws()
        {
            Assert.Throws<ContractFailedException>(() => _mocker.InjectCurrentPositionSaving(null, Instruction.Create(OpCodes.Nop)));
            Assert.Throws<ContractFailedException>(() => _mocker.InjectCurrentPositionSaving(GetILProcessor(), null));
        }

        [Fact]
        public void InjectCurrentPositionSaving_ValidInput_InjectedAfterInstruction()
        {
            var method = _typeInfo.Methods.Single(m => m.Name == nameof(SomeType.SomeMethodWithBody));
            var proc = method.Body.GetILProcessor();
            var cmd = proc.Body.Instructions[1];
            _mocker.MemberInfo.ActualCallsField = new FieldDefinition("Test", FieldAttributes.Private, _typeInfo.Import(typeof(int)));

            _mocker.InjectCurrentPositionSaving(proc, cmd);

            Assert.True(proc.Body.Instructions.Ordered(
                Cil.Cmd(cmd.OpCode, cmd.Operand),
                Cil.Cmd(OpCodes.Ldsfld, _mocker.MemberInfo.ActualCallsField),
                Cil.Cmd(OpCodes.Ldc_I4, _mocker.MemberInfo.SourceCodeCallsCount),
                Cil.Cmd(OpCodes.Callvirt, (MethodReference m) => m.Name == "Add" && m.DeclaringType.Name == "List`1"),
                Cil.AnyCmd() //last instruction, see SomeType::SomeMethodWithBody()
                ));
        }

        [Fact]
        public void PopMethodArguments_Null_Throws()
        {
            Assert.Throws<ContractFailedException>(() => _mocker.PopMethodArguments(null, Instruction.Create(OpCodes.Nop)));
            Assert.Throws<ContractFailedException>(() => _mocker.PopMethodArguments(GetILProcessor(), null));
        }

        private static FakeArgument GetFakeArgument(dynamic value)
            => new FakeArgument(new EqualityArgumentChecker(value));

        private List<FakeArgument> GetSetupArguments()
        {
            return new List<FakeArgument> { GetFakeArgument(0), GetFakeArgument(0) };
        }

        [Fact]
        public void PopMethodArguments_MethodWithTwoArgs_ReturnNewTwoFields()
        {
            var method = _typeInfo.Methods.Single(m => m.Name == nameof(SomeType.SomeMethodWithBody));
            var proc = method.Body.GetILProcessor();
            var cmd = proc.Body.Instructions[1];
            _setup.Method = GetMethodInfo(nameof(SomeType.SomeMethodWithArguments));
            _setup.SetupArguments = GetSetupArguments();

            var fields = _mocker.PopMethodArguments(proc, cmd);

            Assert.Equal(2, fields.OfType<FieldDefinition>().Count());
        }

        [Fact]
        public void PopMethodArguments_MethodWithTwoArgs_CorrectAccessFieldModificators()
        {
            var method = _typeInfo.Methods.Single(m => m.Name == nameof(SomeType.SomeMethodWithBody));
            var proc = method.Body.GetILProcessor();
            var cmd = proc.Body.Instructions[1];
            _setup.Method = GetMethodInfo(nameof(SomeType.SomeMethodWithArguments));
            _setup.SetupArguments = GetSetupArguments();

            var fields = _mocker.PopMethodArguments(proc, cmd);

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
            var method = _typeInfo.Methods.Single(m => m.Name == nameof(SomeType.SomeMethodWithBody));
            var proc = method.Body.GetILProcessor();
            var cmd = proc.Body.Instructions[1];
            _setup.Method = GetMethodInfo(nameof(SomeType.SomeMethodWithArguments));
            _setup.SetupArguments = GetSetupArguments();
            _mocker.MemberInfo.SourceCodeCallsCount = sourceCallsCount;

            var fields = _mocker.PopMethodArguments(proc, cmd);

            Assert.Equal(firstArg, fields[0].Name);
            Assert.Equal(secondArg, fields[1].Name);
        }

        [Fact]
        public void PopMethodArguments_ValidInput_InjectedFieldInitialization()
        {
            var method = _typeInfo.Methods.Single(m => m.Name == nameof(SomeType.SomeMethodWithBody));
            var proc = method.Body.GetILProcessor();
            var cmd = proc.Body.Instructions[1];
            _setup.Method = GetMethodInfo(nameof(SomeType.SomeMethodWithArguments));
            _setup.SetupArguments = GetSetupArguments();

            var fields = _mocker.PopMethodArguments(proc, cmd);

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
            var method = _typeInfo.Methods.Single(m => m.Name == nameof(SomeType.SomeMethodWithTwoObjectArguments));
            var proc = method.Body.GetILProcessor();
            var cmd = proc.Body.Instructions[0];
            _setup.Method = GetMethodInfo(nameof(SomeType.SomeMethodWithTwoObjectArguments));
            _setup.SetupArguments = new List<FakeArgument>{ GetFakeArgument(arg1), GetFakeArgument(arg2) };

            _mocker.PopMethodArguments(proc, cmd);
        }

        [Fact]
        public void RemoveMethodArguments_Null_Throws()
        {
            Assert.Throws<ContractFailedException>(() => _mocker.RemoveMethodArguments(null, Instruction.Create(OpCodes.Nop)));
            Assert.Throws<ContractFailedException>(() => _mocker.RemoveMethodArguments(GetILProcessor(), null));
        }

        [Fact]
        public void RemoveMethodArguments_ValidInput_InjectedArgumentsRemoving()
        {
            var method = _typeInfo.Methods.Single(m => m.Name == nameof(SomeType.SomeMethodWithBody));
            var proc = method.Body.GetILProcessor();
            var cmd = proc.Body.Instructions[1];
            _setup.SetupArguments = GetSetupArguments();

            _mocker.RemoveMethodArguments(proc, cmd);

            Assert.True(proc.Body.Instructions.Ordered(
                Cil.Cmd(OpCodes.Pop),
                Cil.Cmd(OpCodes.Pop),
                Cil.Cmd(cmd.OpCode, cmd.Operand)
                ));
        }

        [Fact]
        public void RemoveStackArgument_Null_Throws()
        {
            Assert.Throws<ContractFailedException>(() => _mocker.RemoveStackArgument(null, Instruction.Create(OpCodes.Nop)));
            Assert.Throws<ContractFailedException>(() => _mocker.RemoveStackArgument(GetILProcessor(), null));
        }

        [Fact]
        public void RemoveStackArgument_ValidInput_InjectedStackArgumentRemoving()
        {
            var method = _typeInfo.Methods.Single(m => m.Name == nameof(SomeType.SomeMethodWithBody));
            var proc = method.Body.GetILProcessor();
            var cmd = proc.Body.Instructions[1];

            _mocker.RemoveStackArgument(proc, cmd);

            Assert.True(proc.Body.Instructions.Ordered(
                Cil.Cmd(OpCodes.Pop),
                Cil.Cmd(cmd.OpCode, cmd.Operand)
                ));
        }

        [Fact]
        public void PushMethodArguments_IncorrectInput_Throws()
        {
            var fields = new[] {new FieldDefinition("Test", FieldAttributes.Private, _typeInfo.Import(typeof(int)))};

            Assert.Throws<ContractFailedException>(
                () => _mocker.PushMethodArguments(null, Instruction.Create(OpCodes.Nop), fields));
            Assert.Throws<ContractFailedException>(
                () => _mocker.PushMethodArguments(GetILProcessor(), null, fields));
            Assert.Throws<ContractFailedException>(
                () => _mocker.PushMethodArguments(GetILProcessor(), Instruction.Create(OpCodes.Nop), null));
            Assert.Throws<ContractFailedException>(
                () => _mocker.PushMethodArguments(GetILProcessor(), Instruction.Create(OpCodes.Nop), Enumerable.Empty<FieldDefinition>()));
        }

        [Fact]
        public void PushMethodArguments_Fields_InjectedArgumentsPushing()
        {
            var fields = new List<FieldDefinition>();
            fields.Add(new FieldDefinition("Test0", FieldAttributes.Private, _typeInfo.Import(typeof(int))));
            fields.Add(new FieldDefinition("Test1", FieldAttributes.Private, _typeInfo.Import(typeof(int))));

            var method = _typeInfo.Methods.Single(m => m.Name == nameof(SomeType.SomeMethodWithBody));
            var proc = method.Body.GetILProcessor();
            var cmd = proc.Body.Instructions[1];

            _mocker.PushMethodArguments(proc, cmd, fields);

            Assert.True(proc.Body.Instructions.Ordered(
                Cil.Cmd(OpCodes.Ldsfld, fields[0]),
                Cil.Cmd(OpCodes.Ldsfld, fields[1]),
                Cil.Cmd(cmd.OpCode, cmd.Operand)
                ));
        }

        [Fact]
        public void MemberInfo_ReturnsCorrectMemberInfo()
        {
            Assert.Equal(_mocker.MemberInfo, ((IMethodMocker)_mocker).MemberInfo);
        }

        [Fact]
        public void RemoveInstruction_Null_Throws()
        {
            Assert.Throws<ContractFailedException>(() => _mocker.RemoveInstruction(null, Instruction.Create(OpCodes.Nop)));
            Assert.Throws<ContractFailedException>(() => _mocker.RemoveInstruction(GetILProcessor(), null));
        }

        [Fact]
        public void RemoveInstruction_ValidInput_InstructionRemoved()
        {
            var method = _typeInfo.Methods.Single(m => m.Name == nameof(SomeType.SomeMethodWithBody));
            var proc = method.Body.GetILProcessor();
            var cmd = proc.Body.Instructions[1];

            _mocker.RemoveInstruction(proc, cmd);

            Assert.DoesNotContain(proc.Body.Instructions, i => i.Equals(cmd));
        }

        [Fact]
        public void ReplaceToRetValueField_Null_Throws()
        {
            Assert.Throws<ContractFailedException>(() => _mocker.ReplaceToRetValueField(null, Instruction.Create(OpCodes.Nop)));
            Assert.Throws<ContractFailedException>(() => _mocker.ReplaceToRetValueField(GetILProcessor(), null));
        }

        [Fact]
        public void ReplaceToRetValueField_ValidInput_InstructionReplaced()
        {
            var method = _typeInfo.Methods.Single(m => m.Name == nameof(SomeType.SomeMethodWithBody));
            _mocker.MemberInfo.RetValueField = new FieldDefinition("Test", FieldAttributes.Private, _typeInfo.Import(typeof(int)));
            var proc = method.Body.GetILProcessor();
            var cmd = proc.Body.Instructions[1];

            _mocker.ReplaceToRetValueField(proc, cmd);

            var replacedCmd = proc.Body.Instructions[1];

            Assert.DoesNotContain(proc.Body.Instructions, i => i.Equals(cmd));
            Assert.Equal(OpCodes.Ldsfld, replacedCmd.OpCode);
            Assert.Equal(_mocker.MemberInfo.RetValueField, replacedCmd.Operand);
        }
    }
}
