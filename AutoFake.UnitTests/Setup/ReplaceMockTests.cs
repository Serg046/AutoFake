using System;
using AutoFake.Exceptions;
using AutoFake.Setup;
using AutoFake.Setup.Mocks;
using AutoFixture.Xunit2;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Moq;
using Xunit;

namespace AutoFake.UnitTests.Setup
{
    public class ReplaceMockTests
    {
        [Theory]
        [InlineAutoMoqData(false, false, false)]
        [InlineAutoMoqData(false, true, true)]
        [InlineAutoMoqData(true, false, true)]
        [InlineAutoMoqData(true, true, true)]
        internal void Inject_NeedCheckArgumentsOrExpectedCallsCountFunc_SaveMethodCall(
            bool needCheckArguments, bool expectedCallsCountFunc, bool mustBeInjected,
            MethodDescriptor descriptor,
            [Frozen]Mock<IProcessor> proc,
            ReplaceMock mock)
        {
            mock.CheckArguments = needCheckArguments;
            mock.ExpectedCalls = expectedCallsCountFunc ? descriptor : null;

            mock.Inject(Mock.Of<IEmitter>(), Nop());

            proc.Verify(m => m.SaveMethodCall(It.IsAny<VariableDefinition>(), needCheckArguments),
                mustBeInjected ? Times.Once() : Times.Never());
        }

        [Theory]
        [InlineAutoMoqData(false, false, true)]
        [InlineAutoMoqData(false, true, false)]
        [InlineAutoMoqData(true, false, false)]
        [InlineAutoMoqData(true, true, false)]
        internal void Inject_ArgsAndNotNeedCheckArguments_ArgumentsRemoved(
            bool needCheckArguments, bool expectedCallsCountFunc, bool mustBeInjected,
            MethodDescriptor descriptor,
            [Frozen]Mock<IProcessor> proc,
            ReplaceMock mock)
        {
            mock.CheckArguments = needCheckArguments;
            mock.ExpectedCalls = expectedCallsCountFunc ? descriptor : null;

            mock.Inject(Mock.Of<IEmitter>(), Nop());

            proc.Verify(m => m.RemoveMethodArgumentsIfAny(), mustBeInjected ? Times.Once() : Times.Never());
        }

        [Theory]
        [InlineAutoMoqData(true)]
        [InlineAutoMoqData(false)]
        internal void Inject_NonStaticMethod_OneStackArgumentMustBeRemoved(
            bool isNonStaticMethod,
            [Frozen]Mock<ISourceMember> sourceMember,
            [Frozen]Mock<IProcessor> proc,
            ReplaceMock mock)
        {
            sourceMember.Setup(m => m.HasStackInstance).Returns(isNonStaticMethod);

            mock.Inject(Mock.Of<IEmitter>(), Nop());

            proc.Verify(m => m.RemoveStackArgument(), isNonStaticMethod ? Times.Once() : Times.Never());
        }

        [Theory]
        [InlineAutoMoqData(true)]
        [InlineAutoMoqData(false)]
        internal void Inject_IsReturnObjectSet_ReplaceToRetValueFieldInjected(
            bool noReturnObject,
            [Frozen]Mock<IProcessor> proc,
            [Frozen]FieldDefinition field,
            MethodDefinition method,
            ReplaceMock mock)
        {
            if (noReturnObject) mock.ReturnObject = null;
            mock.BeforeInjection(method);

            mock.Inject(Mock.Of<IEmitter>(), Nop());

            proc.Verify(m => m.ReplaceToRetValueField(field), noReturnObject ? Times.Never() : Times.Once());
        }

        [Theory]
        [InlineAutoMoqData(true)]
        [InlineAutoMoqData(false)]
        internal void Inject_ReturnObjectIsNotSet_InstructionRemoved(
            bool noReturnObject,
            [Frozen]Mock<IProcessor> proc,
            ReplaceMock mock)
        {
            if (noReturnObject) mock.ReturnObject = null;
            var instruction = Nop();

            mock.Inject(Mock.Of<IEmitter>(), instruction);

            proc.Verify(m => m.RemoveInstruction(instruction), noReturnObject ? Times.Once(): Times.Never());
        }

        [Theory, AutoMoqData]
        internal void Initialize_NoRetValueField_NoEffect(ReplaceMock mock)
        {
            mock.ReturnObject = null;

            Assert.Null(TestClass.RetValueField);
            mock.Initialize(typeof(TestClass));

            Assert.Null(TestClass.RetValueField);
        }

        [Theory, AutoMoqData]
        internal void Initialize_IncorrectRetValueField_Fails(
            [Frozen]Mock<IPrePostProcessor> preProc,
            [Frozen]FieldDefinition field,
            MethodDefinition method,
            ReplaceMock mock)
        {
            field.Name = nameof(TestClass.RetValueField) + "salt";
            var type = typeof(TestClass);
            mock.ReturnObject = new ReplaceMock.Return(new MethodDescriptor(type.FullName, nameof(TestClass.GetValue)));new MethodDescriptor(type.FullName, nameof(TestClass.GetValue));
            mock.BeforeInjection(method);

            Assert.Throws<FakeGeneretingException>(() => mock.Initialize(type));
        }

        [Theory, AutoMoqData]
        internal void Initialize_RetValue_Success(
            [Frozen]Mock<IPrePostProcessor> preProc,
            [Frozen]FieldDefinition field,
            MethodDefinition method,
            ReplaceMock mock)
        {
            field.Name = nameof(TestClass.RetValueField);
            var type = typeof(TestClass);
            mock.ReturnObject = new ReplaceMock.Return(new MethodDescriptor(type.FullName, nameof(TestClass.GetValue)));new MethodDescriptor(type.FullName, nameof(TestClass.GetValue));
            mock.BeforeInjection(method);

            Assert.Null(TestClass.RetValueField);
            mock.Initialize(type);

            Assert.Equal(TestClass.VALUE, TestClass.RetValueField);
            TestClass.RetValueField = null;
        }

        [Theory]
        [InlineAutoMoqData(false, true)]
        [InlineAutoMoqData(true, false)]
        internal void BeforeInjection_ReturnObjectIsSet_RetValueFieldInjected(
            bool noReturnObject,
            bool shouldBeInjected,
            [Frozen]Mock<IPrePostProcessor> proc,
            MethodDefinition method,
            ReplaceMock mock)
        {
            if (noReturnObject) mock.ReturnObject = null;

            mock.BeforeInjection(method);

            proc.Verify(m => m.GenerateRetValueField(It.IsAny<string>(),It.IsAny<Type>()),
                shouldBeInjected ? Times.AtLeastOnce() : Times.Never());
        }

        private static Instruction Nop() => Instruction.Create(OpCodes.Nop);

        private class TestClass
        {
            internal static object RetValueField;

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
