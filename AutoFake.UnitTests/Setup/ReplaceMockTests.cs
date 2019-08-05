using System;
using System.Collections.Generic;
using System.Linq;
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
    public class ReplaceMockTests
    {
        private readonly Mock<IMocker> _mocker;
        private readonly MockedMemberInfo _mockedMemberInfo;
        private readonly ReplaceMock _replaceMock;

        public ReplaceMockTests()
        {
            _mocker = new Mock<IMocker>();
            _mockedMemberInfo = new MockedMemberInfo(null, null);
            _mocker.Setup(m => m.MemberInfo).Returns(_mockedMemberInfo);

            _replaceMock = GetReplaceMock();

            var typeInfo = new TypeInfo(typeof(TestClass), new List<FakeDependency>());
        }

        [Theory]
        [InlineData(false, false, false)]
        [InlineData(false, true, true)]
        [InlineData(true, false, true)]
        [InlineData(true, true, true)]
        public void Inject_NeedCheckArgumentsOrExpectedCallsCountFunc_SaveMethodCall(bool needCheckArguments,
            bool expectedCallsCountFunc, bool mustBeInjected)
        {
            _replaceMock.CheckArguments = needCheckArguments;
            if (expectedCallsCountFunc) _replaceMock.ExpectedCallsFunc = i => i == 0;
            var ilProcessor = GetILProcessor();
            var instruction = GetInstruction();

            _replaceMock.Inject(_mocker.Object, ilProcessor, instruction);

            _mocker.Verify(m => m.SaveMethodCall(ilProcessor, instruction, needCheckArguments),
                mustBeInjected ? Times.Once() : Times.Never());
        }

        [Theory]
        [InlineData(false, false, true)]
        [InlineData(false, true, false)]
        [InlineData(true, false, false)]
        [InlineData(true, true, false)]
        public void Inject_ArgsAndNotNeedCheckArguments_ArgumentsRemoved(bool needCheckArguments,
            bool expectedCallsCountFunc, bool mustBeInjected)
        {
            var ilProcessor = GetILProcessor();
            var instruction = GetInstruction();
            _replaceMock.CheckArguments = needCheckArguments;
            if (expectedCallsCountFunc) _replaceMock.ExpectedCallsFunc = i => i == 0;

            _replaceMock.Inject(_mocker.Object, ilProcessor, instruction);

            _mocker.Verify(m => m.RemoveMethodArgumentsIfAny(ilProcessor, instruction), mustBeInjected ? Times.Once() : Times.Never());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Inject_NonStaticMethod_OneStackArgumentMustBeRemoved(bool isMethodStatic)
        {
            var methodName = isMethodStatic ? nameof(TestClass.StaticTestMethod) : nameof(TestClass.TestMethod);
            var mock = GetReplaceMock(methodName);
            var ilProcessor = GetILProcessor();

            var typeInfo = new TypeInfo(typeof(TestClass), new List<FakeDependency>());
            var methodDefinition = typeInfo.Methods.Single(m => m.Name == methodName);
            var instruction = Instruction.Create(OpCodes.Call, methodDefinition);
            instruction.Operand = methodDefinition;

            mock.Inject(_mocker.Object, ilProcessor, instruction);

            _mocker.Verify(m => m.RemoveStackArgument(ilProcessor, instruction), isMethodStatic ? Times.Never() : Times.Once());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Inject_IsReturnObjectSet_ReplaceToRetValueFieldInjected(bool isReturnObjectSet)
        {
            if (isReturnObjectSet) _replaceMock.ReturnObject = new MethodDescriptor("testType", "testName");
            var ilProcessor = GetILProcessor();
            var instruction = GetInstruction();

            _replaceMock.Inject(_mocker.Object, ilProcessor, instruction);

            _mocker.Verify(m => m.ReplaceToRetValueField(ilProcessor, instruction), isReturnObjectSet ? Times.Once() : Times.Never());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Inject_IsReturnObjectNotSet_InstructionRemoved(bool isReturnObjectSet)
        {
            if (isReturnObjectSet) _replaceMock.ReturnObject = new MethodDescriptor("testType", "testName");
            var ilProcessor = GetILProcessor();
            var instruction = GetInstruction();

            _replaceMock.Inject(_mocker.Object, ilProcessor, instruction);

            _mocker.Verify(m => m.RemoveInstruction(ilProcessor, instruction), isReturnObjectSet ? Times.Never() : Times.Once());
        }

        [Fact]
        public void Initialize_NoRetValueField_NoEffect()
        {
            var mockedMemberInfo = GetMockedMemberInfo();

            _replaceMock.Initialize(mockedMemberInfo, typeof(TestClass));

            Assert.Null(TestClass.RetValueField);
        }

        [Fact]
        public void Initialize_IncorrectRetValueField_Fails()
        {
            _replaceMock.ReturnObject = new MethodDescriptor(typeof(TestClass).FullName, nameof(TestClass.GetValue));
            var mockedMemberInfo = GetMockedMemberInfo();
            mockedMemberInfo.RetValueField = CreateFieldDefinition(nameof(TestClass.RetValueField) + "salt");

            Assert.Throws<FakeGeneretingException>(() => _replaceMock.Initialize(mockedMemberInfo, typeof(TestClass)));
        }

        [Fact]
        public void Initialize_RetValue_Success()
        {
            var type = typeof(TestClass);
            _replaceMock.ReturnObject = new MethodDescriptor(type.FullName, nameof(TestClass.GetValue));
            var mockedMemberInfo = GetMockedMemberInfo();
            mockedMemberInfo.RetValueField = CreateFieldDefinition(nameof(TestClass.RetValueField));

            Assert.Null(TestClass.RetValueField);
            _replaceMock.Initialize(mockedMemberInfo, type);

            Assert.Equal(TestClass.VALUE, TestClass.RetValueField);
            TestClass.RetValueField = null;
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, true)]
        public void BeforeInjection_ReturnObjectIsSet_RetValueFieldInjected(bool isReturnObjectSet, bool shouldBeInjected)
        {
            if (isReturnObjectSet) _replaceMock.ReturnObject = new MethodDescriptor("testType", "testName");
            var mocker = new Mock<IMocker>();

            _replaceMock.BeforeInjection(mocker.Object);

            mocker.Verify(m => m.GenerateRetValueField(It.IsAny<Type>()), shouldBeInjected ? Times.AtLeastOnce() : Times.Never());
        }

        private ILProcessor GetILProcessor() => new MethodBody(null).GetILProcessor();
        private Instruction GetInstruction() => GetInstruction(OpCodes.Call);
        private Instruction GetInstruction(OpCode opCode)
        {
            var type = typeof(TestClass);
            var method = type.GetMethod(nameof(TestClass.TestMethod), BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(int) }, null);
            var typeInfo = new TypeInfo(type, new List<FakeDependency>());
            return Instruction.Create(opCode, typeInfo.Module.Import(method));
        }

        private ISourceMember GetSourceMember(string name)
            => new SourceMethod(typeof(TestClass).GetMethod(name));

        private ReplaceMock GetReplaceMock() => GetReplaceMock(nameof(TestClass.TestMethod));

        private ReplaceMock GetReplaceMock(string methodName)
            => new ReplaceMock(Moq.Mock.Of<IInvocationExpression>(e => e.GetSourceMember() == GetSourceMember(methodName)));

        private FieldDefinition CreateFieldDefinition(string fieldName) => new FieldDefinition(fieldName, Mono.Cecil.FieldAttributes.Static, new FunctionPointerType());

        private FakeArgument CreateArgument(int arg) => new FakeArgument(new EqualityArgumentChecker(arg));

        private MockedMemberInfo GetMockedMemberInfo() => new MockedMemberInfo(_replaceMock, "suffix");

        private class TestClass
        {
            internal static object RetValueField;
            internal static Func<byte, bool> ExpectedCallsFuncField;
            internal static Action CallbackField;

            public void TestMethod(int argument)
            {
                StaticTestMethod();
            }

            public static void StaticTestMethod()
            {
            }

            public const int VALUE = 55;

            internal int GetValue() => VALUE;
        }
    }
}
