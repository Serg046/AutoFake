using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoFake.Exceptions;
using AutoFake.Setup;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Moq;
using Xunit;
using MethodBody = Mono.Cecil.Cil.MethodBody;

namespace AutoFake.UnitTests.Setup.MockTests
{
    public class ReplaceableMockTests
    {
        private const int RET_VALUE_FIELD_VALUE = 55;
        private static readonly Action CALLBACK_FIELD_VALUE = () => { };

        private readonly Mock<IMocker> _mocker;
        private readonly ReplaceableMock.Parameters _parameters;

        private readonly ReplaceableMock _replaceableMock;
        private readonly MockedMemberInfo _mockedMemberInfo;
        private readonly GeneratedObject _generatedObject;

        public ReplaceableMockTests()
        {
            _parameters = new ReplaceableMock.Parameters();
            _mocker = new Mock<IMocker>();
            _mockedMemberInfo = new MockedMemberInfo(null, null, null);
            _mocker.Setup(m => m.MemberInfo).Returns(_mockedMemberInfo);

            _replaceableMock = GetReplaceableMock();

            var typeInfo = new TypeInfo(typeof(TestClass), new List<FakeDependency>());
            _generatedObject = new GeneratedObject(typeInfo);
            _generatedObject.Build();
        }

        private ISourceMember GetSourceMember(string name)
           => new SourceMethod(typeof(TestClass).GetMethod(name));

        private ReplaceableMock GetReplaceableMock()
            => GetReplaceableMock(new List<FakeArgument>() { new FakeArgument(new EqualityArgumentChecker(1)) });

        private ReplaceableMock GetReplaceableMock(string methodName)
            => GetReplaceableMock(methodName, new List<FakeArgument>() { new FakeArgument(new EqualityArgumentChecker(1)) });

        private ReplaceableMock GetReplaceableMock(List<FakeArgument> arguments)
            => GetReplaceableMock(nameof(TestClass.TestMethod), arguments);

        private ReplaceableMock GetReplaceableMock(string methodName, List<FakeArgument> arguments)
            => new ReplaceableMock(GetSourceMember(methodName), arguments, _parameters);

        private FieldDefinition CreateFieldDefinition(string fieldName) => new FieldDefinition(fieldName, Mono.Cecil.FieldAttributes.Static, new FunctionPointerType());

        private FakeArgument CreateArgument(int arg) => new FakeArgument(new EqualityArgumentChecker(arg));
        
        private MockedMemberInfo GetMockedMemberInfo() => new MockedMemberInfo(_replaceableMock, null, "suffix");

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

            _replaceableMock.Inject(_mocker.Object, ilProcessor, instruction);

            _mocker.Verify(m => m.InjectCurrentPositionSaving(ilProcessor, instruction), mustBeInjected ? Times.Once() : Times.Never());
            Assert.Equal(mustBeInjected ? 1 : 0, _mockedMemberInfo.SourceCodeCallsCount);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, true)]
        public void Inject_Callback_CallbackInjected(bool isCallbackSet, bool mustBeInjected)
        {
            if (isCallbackSet)
                _parameters.Callback = () => { };
            var ilProcessor = GetILProcessor();
            var instruction = GetInstruction();

            _replaceableMock.Inject(_mocker.Object, ilProcessor, instruction);

            _mocker.Verify(m => m.InjectCallback(ilProcessor, instruction), mustBeInjected ? Times.Once() : Times.Never());
        }

        [Theory]
        [InlineData(false, false, false)]
        [InlineData(false, true, false)]
        [InlineData(true, false, false)]
        [InlineData(true, true, true)]
        public void Inject_ArgsAndAndNeedCheckArguments_ArgumentsSaved(bool hasArgs, bool needCheckArguments, bool mustBeInjected)
        {
            var ilProcessor = GetILProcessor();
            var instruction = GetInstruction();
            _parameters.NeedCheckArguments = needCheckArguments;
            var runtimeArgs = new List<FieldDefinition>();
            _mocker.Setup(m => m.PopMethodArguments(ilProcessor, instruction)).Returns(runtimeArgs);
            var mock = hasArgs ? GetReplaceableMock() : GetReplaceableMock(new List<FakeArgument>());

            mock.Inject(_mocker.Object, ilProcessor, instruction);

            if (mustBeInjected)
            {
                _mocker.Verify(m => m.PopMethodArguments(ilProcessor, instruction), Times.Once());
                Assert.Equal(1, _mockedMemberInfo.ArgumentsCount);
                Assert.Equal(runtimeArgs, _mockedMemberInfo.GetArguments(0));
            }
            else
            {
                _mocker.Verify(m => m.PopMethodArguments(ilProcessor, instruction), Times.Never);
            }
        }

        [Theory]
        [InlineData(false, false, false)]
        [InlineData(false, true, false)]
        [InlineData(true, false, true)]
        [InlineData(true, true, false)]
        public void Inject_ArgsAndAndNotNeedCheckArguments_ArgumentsRemoved(bool hasArgs, bool needCheckArguments,
            bool mustBeInjected)
        {
            var ilProcessor = GetILProcessor();
            var instruction = GetInstruction();
            _parameters.NeedCheckArguments = needCheckArguments;
            var mock = hasArgs ? GetReplaceableMock() : GetReplaceableMock(new List<FakeArgument>());

            mock.Inject(_mocker.Object, ilProcessor, instruction);

            _mocker.Verify(m => m.RemoveMethodArguments(ilProcessor, instruction), mustBeInjected ? Times.Once() : Times.Never());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Inject_NonStaticMethod_OneStackArgumentMustBeRemoved(bool isMethodStatic)
        {
            var methodName = isMethodStatic ? nameof(TestClass.StaticTestMethod) : nameof(TestClass.TestMethod);
            var mock = GetReplaceableMock(methodName);
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
            if (isReturnObjectSet)
                _parameters.ReturnObject = null;
            var ilProcessor = GetILProcessor();
            var instruction = GetInstruction();

            _replaceableMock.Inject(_mocker.Object, ilProcessor, instruction);

            _mocker.Verify(m => m.ReplaceToRetValueField(ilProcessor, instruction), isReturnObjectSet ? Times.Once() : Times.Never());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Inject_IsReturnObjectNotSet_InstructionRemoved(bool isReturnObjectSet)
        {
            if (isReturnObjectSet)
                _parameters.ReturnObject = null;
            var ilProcessor = GetILProcessor();
            var instruction = GetInstruction();

            _replaceableMock.Inject(_mocker.Object, ilProcessor, instruction);

            _mocker.Verify(m => m.RemoveInstruction(ilProcessor, instruction), isReturnObjectSet ? Times.Never() : Times.Once());
        }

        [Fact]
        public void Initialize_ValidData_Success()
        {
            _parameters.ReturnObject = RET_VALUE_FIELD_VALUE;
            _parameters.Callback = CALLBACK_FIELD_VALUE;
            var mockedMemberInfo = GetMockedMemberInfo();
            mockedMemberInfo.RetValueField = CreateFieldDefinition(nameof(TestClass.RetValueField));
            mockedMemberInfo.CallbackField = CreateFieldDefinition(nameof(TestClass.CallbackField));

            _replaceableMock.Initialize(mockedMemberInfo, _generatedObject);

            var retValueField = _generatedObject.Type
                .GetField(nameof(TestClass.RetValueField), BindingFlags.NonPublic | BindingFlags.Static);
            var callbackFieldField = _generatedObject.Type
                .GetField(nameof(TestClass.CallbackField), BindingFlags.NonPublic | BindingFlags.Static);

            Assert.Equal(CALLBACK_FIELD_VALUE, callbackFieldField.GetValue(_generatedObject.Instance));
            Assert.Equal(RET_VALUE_FIELD_VALUE, retValueField.GetValue(_generatedObject.Instance));
        }

        [Fact]
        public void Initialize_NoRetValueField_Fails()
        {
            _parameters.ReturnObject = RET_VALUE_FIELD_VALUE;
            var mockedMemberInfo = GetMockedMemberInfo();
            mockedMemberInfo.RetValueField = CreateFieldDefinition(nameof(TestClass.RetValueField) + "salt");

            Assert.Throws<FakeGeneretingException>(() => _replaceableMock.Initialize(mockedMemberInfo, _generatedObject));
        }

        [Fact]
        public void Initialize_RetValue_Success()
        {
            _parameters.ReturnObject = RET_VALUE_FIELD_VALUE;
            var mockedMemberInfo = GetMockedMemberInfo();
            mockedMemberInfo.RetValueField = CreateFieldDefinition(nameof(TestClass.RetValueField));

            _replaceableMock.Initialize(mockedMemberInfo, _generatedObject);

            var retValueField = _generatedObject.Type
                .GetField(nameof(TestClass.RetValueField), BindingFlags.NonPublic | BindingFlags.Static);
            Assert.Equal(RET_VALUE_FIELD_VALUE, retValueField.GetValue(_generatedObject.Instance));
        }

        [Fact]
        public void Initialize_NoCallbackField_Fails()
        {
            _parameters.Callback = CALLBACK_FIELD_VALUE;
            var mockedMemberInfo = GetMockedMemberInfo();
            mockedMemberInfo.CallbackField = CreateFieldDefinition(nameof(TestClass.CallbackField) + "salt");

            Assert.Throws<FakeGeneretingException>(() => _replaceableMock.Initialize(mockedMemberInfo, _generatedObject));
        }

        [Fact]
        public void Initialize_Callback_Success()
        {
            _parameters.Callback = CALLBACK_FIELD_VALUE;
            var mockedMemberInfo = GetMockedMemberInfo();
            mockedMemberInfo.CallbackField = CreateFieldDefinition(nameof(TestClass.CallbackField));

            _replaceableMock.Initialize(mockedMemberInfo, _generatedObject);

            var callbackFieldField = _generatedObject.Type
                .GetField(nameof(TestClass.CallbackField), BindingFlags.NonPublic | BindingFlags.Static);
            Assert.Equal(CALLBACK_FIELD_VALUE, callbackFieldField.GetValue(_generatedObject.Instance));
        }

        [Theory]
        [InlineData(false,false,false)]
        [InlineData(false,true,true)]
        [InlineData(true,false,true)]
        [InlineData(true,true,true)]
        public void PrepareForInjecting_NeedCheckArgumentsOrExpectedCallsCount_CallsCounterInjected(
            bool needCheckArguments, bool expectedCallsCount, bool shouldBeInjected)
        {
            _parameters.NeedCheckArguments = needCheckArguments;
            _parameters.ExpectedCallsCountFunc = expectedCallsCount ? i => i == 1 : (Func<int, bool>)null;
            var mocker = new Mock<IMocker>();

            _replaceableMock.PrepareForInjecting(mocker.Object);

            mocker.Verify(m => m.GenerateCallsCounter(), shouldBeInjected ? Times.AtLeastOnce() : Times.Never());
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, true)]
        public void PrepareForInjecting_ReturnObjectIsSet_RetValueFieldInjected(bool isReturnObjectSet, bool shouldBeInjected)
        {
            if (isReturnObjectSet)
                _parameters.ReturnObject = null;
            var mocker = new Mock<IMocker>();

            _replaceableMock.PrepareForInjecting(mocker.Object);

            mocker.Verify(m => m.GenerateRetValueField(), shouldBeInjected ? Times.AtLeastOnce() : Times.Never());
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, true)]
        public void PrepareForInjecting_CallbackIsSet_CallbackFieldInjected(bool isCallbackSet, bool shouldBeInjected)
        {
            if (isCallbackSet)
                _parameters.Callback = CALLBACK_FIELD_VALUE;
            var mocker = new Mock<IMocker>();

            _replaceableMock.PrepareForInjecting(mocker.Object);

            mocker.Verify(m => m.GenerateCallbackField(), shouldBeInjected ? Times.AtLeastOnce() : Times.Never());
        }

        private ILProcessor GetILProcessor() => new MethodBody(null).GetILProcessor();
        private Instruction GetInstruction() => GetInstruction(OpCodes.Call);
        private Instruction GetInstruction(OpCode opCode)
        {
            var type = typeof(TestClass);
            var method = type.GetMethod(nameof(TestClass.TestMethod), BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(int) }, null);
            var typeInfo = new TypeInfo(type, new List<FakeDependency>());
            return Instruction.Create(opCode, typeInfo.Import(method));
        }

        private class TestClass
        {
            internal static object RetValueField;
            internal static Action CallbackField;

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
