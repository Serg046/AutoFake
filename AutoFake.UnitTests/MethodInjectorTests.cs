using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoFake.Exceptions;
using AutoFake.Setup;
using GuardExtensions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Moq;
using Xunit;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using MethodBody = Mono.Cecil.Cil.MethodBody;

namespace AutoFake.UnitTests
{
    public class MethodInjectorTests
    {
        private class SomeType
        {
            public void InstanceMethod()
            {
            }
        }

        private void InstanceMethod()
        {
        }

        private void InstanceMethod(int overload)
        {
        }

        private static void StaticMethod()
        {
        }

        private void MethodWithBody()
        {
            InstanceMethod();
            var x = DateTime.Now;
            StaticMethod();
        }

        private void MethodWithOverloadedMethod()
        {
            InstanceMethod();
            var x = DateTime.Now;
            InstanceMethod(0);
        }

        private void MethodWithExternalMethodOfNestedType()
        {
            InstanceMethod();
            var x = DateTime.Now;
            new SomeType().InstanceMethod();
        }

        private ILProcessor GetILProcessor() => new MethodBody(null).GetILProcessor();

        private Instruction GetInstruction()
        {
            var type = GetType();
            var method = type.GetMethod(nameof(InstanceMethod), BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[0], null);
            var typeInfo = new TypeInfo(type, null);
            typeInfo.Load();
            return Instruction.Create(OpCodes.Call, typeInfo.Import(method));
        }

        private readonly FakeSetupPack _setup;
        private readonly Mock<IMethodMocker> _methodMockerMock;
        private readonly MockedMemberInfo _memberInfo;
        private readonly MethodInjector _methodInjector;

        public MethodInjectorTests()
        {
            _setup = new FakeSetupPack();
            _setup.SetupArguments = new object[0];

            _memberInfo = new MockedMemberInfo(_setup);

            _methodMockerMock = new Mock<IMethodMocker>();
            _methodMockerMock.Setup(m => m.MemberInfo).Returns(_memberInfo);
            _methodInjector = new MethodInjector(_methodMockerMock.Object);
        }

        [Fact]
        public void Ctor_Null_Throws()
        {
            Assert.Throws<ContractFailedException>(() => new MethodInjector(null));
        }

        [Fact]
        public void Process_InvalidInput_Throws()
        {
            Assert.Throws<ContractFailedException>(() => _methodInjector.Process(null, GetInstruction()));
            Assert.Throws<ContractFailedException>(() => _methodInjector.Process(GetILProcessor(), null));
            Assert.Throws<ContractFailedException>(() => _methodInjector.Process(GetILProcessor(), Instruction.Create(OpCodes.Nop)));
        }

        [Fact]
        public void Process_ValidInput_SavesCurrentPosition()
        {
            var cmd = GetInstruction();
            var proc = GetILProcessor();

            _methodInjector.Process(proc, cmd);

            _methodMockerMock.Verify(m => m.InjectCurrentPositionSaving(proc, cmd));
        }

        [Theory]
        [InlineData(null, false, false)]
        [InlineData(null, true, false)]
        [InlineData(1, false, false)]
        [InlineData(1, true, true)]
        public void Process_NeedCheckArgumentsAndThereAreArgs_GetsArguments(object arg, bool needCheckArgs, bool mustBePopped)
        {
            var cmd = GetInstruction();
            var proc = GetILProcessor();
            if (arg != null)
                _setup.SetupArguments = new [] {arg};
            _setup.NeedCheckArguments = needCheckArgs;

            _methodInjector.Process(proc, cmd);

            if (mustBePopped)
                _methodMockerMock.Verify(m => m.PopMethodArguments(proc, cmd));
            else
                _methodMockerMock.Verify(m => m.PopMethodArguments(proc, cmd), Times.Never);
        }

        [Theory]
        [InlineData(null, false, false, false)]
        [InlineData(null, false, true, false)]
        [InlineData(null, true, false, false)]
        [InlineData(null, true, true, false)]
        [InlineData(1, false, false, false)]
        [InlineData(1, false, true, false)]
        [InlineData(1, true, false, false)]
        [InlineData(1, true, true, true)]
        public void Process_NeedCheckArgumentsAndThereAreArgsAndIsVerification_PushArguments(object arg,
            bool needCheckArgs, bool justVerification, bool mustBePushedBack)
        {
            var cmd = GetInstruction();
            var proc = GetILProcessor();
            if (arg != null)
                _setup.SetupArguments = new[] { arg };
            _setup.NeedCheckArguments = needCheckArgs;
            _setup.IsVerification = justVerification;

            _methodInjector.Process(proc, cmd);

            if (mustBePushedBack)
                _methodMockerMock.Verify(m => m.PushMethodArguments(proc, cmd, It.IsAny<IEnumerable<FieldDefinition>>()));
            else
                _methodMockerMock.Verify(m => m.PushMethodArguments(proc, cmd, It.IsAny<IEnumerable<FieldDefinition>>()), Times.Never);
        }

        [Theory]
        [InlineData(null, false, false)]
        [InlineData(null, true, false)]
        [InlineData(1, false, false)]
        [InlineData(1, true, true)]
        public void Process_NeedCheckArguments_ArgumetsSaved(object arg, bool needCheckArgs, bool mustBeSaved)
        {
            var cmd = GetInstruction();
            var proc = GetILProcessor();
            if (arg != null)
                _setup.SetupArguments = new[] { arg };
            _setup.NeedCheckArguments = needCheckArgs;

            var fields = new List<FieldDefinition>();
            fields.Add(new FieldDefinition("Test0", FieldAttributes.Private, new FunctionPointerType()));
            fields.Add(new FieldDefinition("Test1", FieldAttributes.Private, new FunctionPointerType()));

            _methodMockerMock.Setup(m => m.PopMethodArguments(proc, cmd)).Returns(fields);

            _methodInjector.Process(proc, cmd);

            if (mustBeSaved)
            {
                Assert.Equal(fields, _memberInfo.GetArguments(0));
                Assert.Throws<ArgumentOutOfRangeException>(() => _memberInfo.GetArguments(1));
            }
            else
                Assert.Throws<ArgumentOutOfRangeException>(() => _memberInfo.GetArguments(0));
        }

        [Theory]
        [InlineData(null, false, false, false)]
        [InlineData(null, false, true, false)]
        [InlineData(null, true, false, false)]
        [InlineData(null, true, true, false)]
        [InlineData(1, false, false, true)]
        [InlineData(1, false, true, false)]
        [InlineData(1, true, false, false)]
        [InlineData(1, true, true, false)]
        public void Process_SomeArgsAndNotNeedCheck_ArgumentsRemoved(object arg, bool needCheckArgs, bool isVerification, bool mustBeRemoved)
        {
            var cmd = GetInstruction();
            var proc = GetILProcessor();
            if (arg != null)
                _setup.SetupArguments = new[] { arg };
            _setup.NeedCheckArguments = needCheckArgs;
            _setup.IsVerification = isVerification;

            _methodInjector.Process(proc, cmd);

            if (mustBeRemoved)
                _methodMockerMock.Verify(m => m.RemoveMethodArguments(proc, cmd));
            else
                _methodMockerMock.Verify(m => m.RemoveMethodArguments(proc, cmd), Times.Never);
        }

        [Theory]
        [InlineData(false, false, true)]
        [InlineData(false, true, false)]
        [InlineData(true, true, false)]
        public void Process_IsNotVerificationAndNotStatic_RemovedStackArgument(bool isVerification, bool isStatic, bool mustBeRemoved)
        {
            _setup.IsVerification = isVerification;
            var methodName = isStatic ? nameof(StaticMethod) : nameof(InstanceMethod);
            var type = GetType();
            var method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static,
                null, new Type[0], null);
            var typeInfo = new TypeInfo(type, null);
            typeInfo.Load();
            var cmd = Instruction.Create(OpCodes.Call, typeInfo.Import(method));
            var proc = GetILProcessor();

            _methodInjector.Process(proc, cmd);

            if (mustBeRemoved)
                _methodMockerMock.Verify(m => m.RemoveStackArgument(proc, cmd));
            else
                _methodMockerMock.Verify(m => m.RemoveStackArgument(proc, cmd), Times.Never);
        }

        [Fact]
        public void Process_IsNotVerificationAndNotStaticAndIncorrectInstruction_Throws()
        {
            var typeInfo = new TypeInfo(GetType(), null);
            typeInfo.Load();
            var cmd = Instruction.Create(OpCodes.Call, typeInfo.Methods.First());
            cmd.Operand = typeInfo.Fields.First();
            var proc = GetILProcessor();

            Assert.Throws<FakeGeneretingException>(() => _methodInjector.Process(proc, cmd));
        }

        [Theory]
        [InlineData(false, false, false)]
        [InlineData(false, true, true)]
        [InlineData(true, false, false)]
        [InlineData(true, true, false)]
        public void Process_IsNotVerificationAndIsVoid_InstructionRemoved(bool isVerification, bool isVoid, bool mustBeRemoved)
        {
            var cmd = GetInstruction();
            var proc = GetILProcessor();
            _setup.IsVerification = isVerification;
            _setup.IsVoid = isVoid;

            _methodInjector.Process(proc, cmd);

            if (mustBeRemoved)
                _methodMockerMock.Verify(m => m.RemoveInstruction(proc, cmd));
            else
                _methodMockerMock.Verify(m => m.RemoveInstruction(proc, cmd), Times.Never);
        }

        [Theory]
        [InlineData(false, false, true)]
        [InlineData(false, true, false)]
        [InlineData(true, false, false)]
        [InlineData(true, true, false)]
        public void Process_IsNotVerificationAndIsNotVoid_InstructionRemoved(bool isVerification, bool isVoid, bool mustBeReplaced)
        {
            var cmd = GetInstruction();
            var proc = GetILProcessor();
            _setup.IsVerification = isVerification;
            _setup.IsVoid = isVoid;

            _methodInjector.Process(proc, cmd);

            if (mustBeReplaced)
                _methodMockerMock.Verify(m => m.ReplaceToRetValueField(proc, cmd));
            else
                _methodMockerMock.Verify(m => m.ReplaceToRetValueField(proc, cmd), Times.Never);
        }

        [Fact]
        public void Process_ValidInput_SourceCodeCallsIncremented()
        {
            _methodInjector.Process(GetILProcessor(), GetInstruction());
            Assert.Equal(1, _memberInfo.SourceCodeCallsCount);

            _methodInjector.Process(GetILProcessor(), GetInstruction());
            Assert.Equal(2, _memberInfo.SourceCodeCallsCount);
        }

        [Fact]
        public void IsInstalledMethod_Null_Throws()
        {
            Assert.Throws<ContractFailedException>(() => _methodInjector.IsInstalledMethod(null));
        }

        [Fact]
        public void IsInstalledMethod_ValidInput_Success()
        {
            var typeInfo = new TypeInfo(GetType(), null);
            typeInfo.Load();
            _methodMockerMock.SetupGet(m => m.TypeInfo).Returns(typeInfo);
            var method = typeInfo.Methods.Single(m => m.Name == nameof(MethodWithBody));

            _setup.Method = typeof(DateTime).GetProperty(nameof(DateTime.Now)).GetMethod;

            var instructions = method.Body.GetILProcessor().Body.Instructions
                .Where(i => i.OpCode.OperandType == OperandType.InlineMethod)
                .ToList();

            Assert.Equal(3, instructions.Count);
            Assert.False(_methodInjector.IsInstalledMethod((dynamic)instructions[0].Operand));
            Assert.True(_methodInjector.IsInstalledMethod((dynamic)instructions[1].Operand));
            Assert.False(_methodInjector.IsInstalledMethod((dynamic)instructions[2].Operand));
        }

        [Fact]
        public void IsInstalledMethod_InternalInstalledMethod_Success()
        {
            var typeInfo = new TypeInfo(GetType(), null);
            typeInfo.Load();
            _methodMockerMock.SetupGet(m => m.TypeInfo).Returns(typeInfo);
            var method = typeInfo.Methods.Single(m => m.Name == nameof(MethodWithBody));

            _setup.Method = GetType().GetMethod(nameof(InstanceMethod), BindingFlags.Instance | BindingFlags.NonPublic,
                null, new Type[0], null);

            var instructions = method.Body.GetILProcessor().Body.Instructions
                .Where(i => i.OpCode.OperandType == OperandType.InlineMethod)
                .ToList();

            Assert.Equal(3, instructions.Count);
            Assert.True(_methodInjector.IsInstalledMethod((dynamic)instructions[0].Operand));
            Assert.False(_methodInjector.IsInstalledMethod((dynamic)instructions[1].Operand));
            Assert.False(_methodInjector.IsInstalledMethod((dynamic)instructions[2].Operand));
        }

        [Fact]
        public void IsInstalledMethod_OverloadedInstalledMethod_Success()
        {
            var typeInfo = new TypeInfo(GetType(), null);
            typeInfo.Load();
            _methodMockerMock.SetupGet(m => m.TypeInfo).Returns(typeInfo);
            var method = typeInfo.Methods.Single(m => m.Name == nameof(MethodWithOverloadedMethod));

            _setup.Method = GetType().GetMethod(nameof(InstanceMethod), BindingFlags.Instance | BindingFlags.NonPublic,
                null, new [] {typeof(int)}, null);

            var instructions = method.Body.GetILProcessor().Body.Instructions
                .Where(i => i.OpCode.OperandType == OperandType.InlineMethod)
                .ToList();

            Assert.Equal(3, instructions.Count);
            Assert.False(_methodInjector.IsInstalledMethod((dynamic)instructions[0].Operand));
            Assert.False(_methodInjector.IsInstalledMethod((dynamic)instructions[1].Operand));
            Assert.True(_methodInjector.IsInstalledMethod((dynamic)instructions[2].Operand));
        }

        [Fact]
        public void IsInstalledMethod_MethodFromExternalNestedType_Success()
        {
            var typeInfo = new TypeInfo(GetType(), null);
            typeInfo.Load();
            _methodMockerMock.SetupGet(m => m.TypeInfo).Returns(typeInfo);
            var method = typeInfo.Methods.Single(m => m.Name == nameof(MethodWithExternalMethodOfNestedType));

            _setup.Method = typeof(SomeType).GetMethod(nameof(InstanceMethod));

            var instructions = method.Body.GetILProcessor().Body.Instructions
                .Where(i => i.OpCode.OperandType == OperandType.InlineMethod && i.OpCode != OpCodes.Newobj)
                .ToList();

            Assert.Equal(3, instructions.Count);
            Assert.False(_methodInjector.IsInstalledMethod((dynamic)instructions[0].Operand));
            Assert.False(_methodInjector.IsInstalledMethod((dynamic)instructions[1].Operand));
            Assert.True(_methodInjector.IsInstalledMethod((dynamic)instructions[2].Operand));
        }
    }
}
