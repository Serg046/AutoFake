using System;
using System.Collections.Generic;
using System.Reflection;
using AutoFake.Expression;
using AutoFake.Setup;
using Mono.Cecil.Cil;
using Moq;
using Xunit;
using MethodBody = Mono.Cecil.Cil.MethodBody;

namespace AutoFake.UnitTests.Setup.MockTests
{
    public class VerifiableMockTests
    {
        private readonly VerifiableMock.Parameters _parameters;
        private readonly Mock<IMocker> _mocker;

        private readonly VerifiableMock _verifiableMock;

        public VerifiableMockTests()
        {
            _parameters = new VerifiableMock.Parameters();
            _mocker = new Mock<IMocker>();
            var mockedMemberInfo = new MockedMemberInfo(null, null, null);
            _mocker.Setup(m => m.MemberInfo).Returns(mockedMemberInfo);

            _verifiableMock = GetVerifiableMock();
        }

        [Theory]
        [InlineData(false, false, false)]
        [InlineData(false, true, true)]
        [InlineData(true, false, true)]
        [InlineData(true, true, true)]
        public void Inject_NeedCheckArgumentsOrExpectedCallsCountFunc_CallsCounterInjected(bool needCheckArguments,
            bool expectedCallsCountFunc, bool mustBeInjected)
        {
            _parameters.NeedCheckArguments = needCheckArguments;
            if (expectedCallsCountFunc) _parameters.ExpectedCallsCountFunc = i => i == 0;
            var ilProcessor = GetILProcessor();
            var instruction = GetInstruction();

            _verifiableMock.Inject(_mocker.Object, ilProcessor, instruction);

            _mocker.Verify(m => m.SaveMethodCall(ilProcessor, instruction), mustBeInjected ? Times.Once() : Times.Never());
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
            if (expectedCallsCountFunc) _parameters.ExpectedCallsCountFunc = i => i == 0;
            _parameters.NeedCheckArguments = needCheckArguments;
            var runtimeArgs = new List<VariableDefinition>();
            _mocker.Setup(m => m.SaveMethodCall(ilProcessor, instruction)).Returns(runtimeArgs);

            _verifiableMock.Inject(_mocker.Object, ilProcessor, instruction);

            if (mustBeInjected)
            {
                _mocker.Verify(m => m.SaveMethodCall(ilProcessor, instruction), Times.Once());
                _mocker.Verify(m => m.PushMethodArguments(ilProcessor, instruction, runtimeArgs), Times.Once());
            }
            else
            {
                _mocker.Verify(m => m.SaveMethodCall(ilProcessor, instruction), Times.Never);
                _mocker.Verify(m => m.PushMethodArguments(ilProcessor, instruction, runtimeArgs), Times.Never);
            }
        }

        [Theory]
        [InlineData(false, false, false)]
        [InlineData(false, true, true)]
        [InlineData(true, false, false)]
        [InlineData(true, true, true)]
        public void PrepareForInjecting_NeedCheckArgumentsOrExpectedCallsCount_CallsCounterInjected(
            bool needCheckArguments, bool expectedCallsCount, bool shouldBeInjected)
        {
            _parameters.NeedCheckArguments = needCheckArguments;
            _parameters.ExpectedCallsCountFunc = expectedCallsCount ? i => i == 1 : (Func<byte, bool>)null;
            var mocker = new Mock<IMocker>();

            _verifiableMock.PrepareForInjecting(mocker.Object);

            mocker.Verify(m => m.GenerateCallsCounterFuncField(), shouldBeInjected ? Times.AtLeastOnce() : Times.Never());
        }

        private VerifiableMock GetVerifiableMock()
            => new VerifiableMock(Moq.Mock.Of<IInvocationExpression>(e => e.GetSourceMember() == GetSourceMember()), _parameters);

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
