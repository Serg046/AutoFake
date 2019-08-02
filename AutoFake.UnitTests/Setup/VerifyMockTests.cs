using System;
using System.Collections.Generic;
using System.Reflection;
using AutoFake.Exceptions;
using AutoFake.Expression;
using AutoFake.Setup;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Moq;
using Xunit;
using MethodBody = Mono.Cecil.Cil.MethodBody;

namespace AutoFake.UnitTests.Setup
{
    public class VerifyMockTests
    {
        private readonly Mock<IMocker> _mocker;
        private readonly VerifyMock _verifyMock;

        public VerifyMockTests()
        {
            _mocker = new Mock<IMocker>();
            var mockedMemberInfo = new MockedMemberInfo(null, null);
            _mocker.Setup(m => m.MemberInfo).Returns(mockedMemberInfo);

            _verifyMock = GetVerifyMock();
        }

        [Theory]
        [InlineData(false, false, false)]
        [InlineData(false, true, true)]
        [InlineData(true, false, true)]
        [InlineData(true, true, true)]
        public void InjectNeedCheckArgumentsOrExpectedCallsCount_ArgumentsSaved(bool needCheckArguments,
            bool expectedCallsCountFunc, bool mustBeInjected)
        {
            var ilProcessor = GetILProcessor();
            var instruction = GetInstruction();
            if (expectedCallsCountFunc) _verifyMock.ExpectedCallsFunc = i => i == 0;
            _verifyMock.CheckArguments = needCheckArguments;
            var runtimeArgs = new List<VariableDefinition>();
            _mocker.Setup(m => m.SaveMethodCall(ilProcessor, instruction, needCheckArguments)).Returns(runtimeArgs);

            _verifyMock.Inject(_mocker.Object, ilProcessor, instruction);

            if (mustBeInjected)
            {
                _mocker.Verify(m => m.SaveMethodCall(ilProcessor, instruction, needCheckArguments), Times.Once());
                _mocker.Verify(m => m.PushMethodArguments(ilProcessor, instruction, runtimeArgs), Times.Once());
            }
            else
            {
                _mocker.Verify(m => m.SaveMethodCall(ilProcessor, instruction, needCheckArguments), Times.Never);
                _mocker.Verify(m => m.PushMethodArguments(ilProcessor, instruction, runtimeArgs), Times.Never);
            }
        }

        [Fact]
        public void Initialize_NoExpectedCallsField_NoEffect()
        {
            var mockedMemberInfo = new MockedMemberInfo(GetVerifyMock(), null);

            _verifyMock.Initialize(mockedMemberInfo, typeof(TestClass));

            Assert.Null(TestClass.ExpectedCallsFuncField);
        }

        [Fact]
        public void Initialize_IncorrectExpectedCallsField_Fails()
        {
            _verifyMock.ExpectedCallsFunc = i => true;
            var mockedMemberInfo = new MockedMemberInfo(GetVerifyMock(), null);
            mockedMemberInfo.ExpectedCallsFuncField = new FieldDefinition(nameof(TestClass.ExpectedCallsFuncField) + "salt",
                Mono.Cecil.FieldAttributes.Assembly, new FunctionPointerType());

            Assert.Throws<FakeGeneretingException>(() => _verifyMock.Initialize(mockedMemberInfo, typeof(TestClass)));
        }

        [Fact]
        public void Initialize_ExpectedCallsFunc_Set()
        {
            var type = typeof(TestClass);
            _verifyMock.ExpectedCallsFunc = i => true;
            var mockedMemberInfo = new MockedMemberInfo(GetVerifyMock(), null);
            mockedMemberInfo.ExpectedCallsFuncField = new FieldDefinition(nameof(TestClass.ExpectedCallsFuncField),
                Mono.Cecil.FieldAttributes.Assembly, new FunctionPointerType());

            Assert.Null(TestClass.ExpectedCallsFuncField);
            _verifyMock.Initialize(mockedMemberInfo, type);

            Assert.Equal(_verifyMock.ExpectedCallsFunc, TestClass.ExpectedCallsFuncField);
            TestClass.ExpectedCallsFuncField = null;
        }

        [Theory]
        [InlineData(false, false, false)]
        [InlineData(false, true, true)]
        [InlineData(true, false, true)]
        [InlineData(true, true, true)]
        public void PrepareForInjecting_NeedCheckArgumentsOrExpectedCallsCount_GenerateSetupBodyFieldInjected(
            bool needCheckArguments, bool expectedCallsCount, bool shouldBeInjected)
        {
            _verifyMock.CheckArguments = needCheckArguments;
            _verifyMock.ExpectedCallsFunc = expectedCallsCount ? i => i == 1 : (Func<byte, bool>)null;
            var mocker = new Mock<IMocker>();

            _verifyMock.BeforeInjection(mocker.Object);

            mocker.Verify(m => m.GenerateSetupBodyField(), shouldBeInjected ? Times.AtLeastOnce() : Times.Never());
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, true)]
        public void PrepareForInjecting_ExpectedCallsFunc_Injected(bool callsCounter, bool shouldBeInjected)
        {
            if (callsCounter) _verifyMock.ExpectedCallsFunc = i => true;
            var mocker = new Mock<IMocker>();

            _verifyMock.BeforeInjection(mocker.Object);

            mocker.Verify(m => m.GenerateCallsCounterFuncField(), shouldBeInjected ? Times.AtLeastOnce() : Times.Never());
        }

        private VerifyMock GetVerifyMock() => new VerifyMock(
            Moq.Mock.Of<IInvocationExpression>(e => e.GetSourceMember() == GetSourceMember()));

        private ISourceMember GetSourceMember()
            => new SourceMethod(typeof(TestClass).GetMethod(nameof(TestClass.TestMethod)));

        private ILProcessor GetILProcessor() => new MethodBody(null).GetILProcessor();
        private Instruction GetInstruction() => GetInstruction(OpCodes.Call);
        private Instruction GetInstruction(OpCode opCode)
        {
            var type = typeof(TestClass);
            var method = type.GetMethod(nameof(TestClass.TestMethod), BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(int) }, null);
            var typeInfo = new TypeInfo(type, new List<FakeDependency>());
            return Instruction.Create(opCode, typeInfo.Module.Import(method));
        }

        private class TestClass
        {
            internal static Func<byte, bool> ExpectedCallsFuncField;

            public void TestMethod(int argument)
            {
                StaticTestMethod();
            }

            public static void StaticTestMethod()
            {
            }
        }
    }
}
