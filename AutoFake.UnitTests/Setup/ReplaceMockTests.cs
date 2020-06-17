using System;
using System.Collections.Generic;
using AutoFake.Exceptions;
using AutoFake.Expression;
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
            [Frozen]Mock<IProcessor> proc,
            IProcessorFactory processorFactory,
            Mock<IInvocationExpression> expression)
        {
            expression.Setup(e => e.GetArguments()).Returns(new List<IFakeArgument>
            {
                new FakeArgument(needCheckArguments
                    ? new EqualityArgumentChecker(1)
                    : (IFakeArgumentChecker)new SuccessfulArgumentChecker())
            });
            var mock = new ReplaceMock(processorFactory, expression.Object);
            mock.ExpectedCalls = expectedCallsCountFunc ? new Func<byte, bool>(i => true) : null;

            mock.Inject(Mock.Of<IEmitter>(), Nop());

            proc.Verify(m => m.SaveMethodCall(It.IsAny<FieldDefinition>(), needCheckArguments),
                mustBeInjected ? Times.Once() : Times.Never());
        }

        [Theory]
        [InlineAutoMoqData(false, false, true)]
        [InlineAutoMoqData(false, true, false)]
        [InlineAutoMoqData(true, false, false)]
        [InlineAutoMoqData(true, true, false)]
        internal void Inject_ArgsAndNotNeedCheckArguments_ArgumentsRemoved(
            bool needCheckArguments, bool expectedCallsCountFunc, bool mustBeInjected,
            [Frozen]Mock<IProcessor> proc,
            IProcessorFactory processorFactory,
            Mock<IInvocationExpression> expression)
        {
            expression.Setup(e => e.GetArguments()).Returns(new List<IFakeArgument>
            {
                new FakeArgument(needCheckArguments
                    ? new EqualityArgumentChecker(1)
                    : (IFakeArgumentChecker)new SuccessfulArgumentChecker())
            });
            var mock = new ReplaceMock(processorFactory, expression.Object);
            mock.ExpectedCalls = expectedCallsCountFunc ? new Func<byte, bool>(i => true) : null; 

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
            mock.ExpectedCalls = null;

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
            preProc.Setup(p => p.GenerateField(It.IsAny<string>(), It.IsAny<Type>())).Returns(field);
            field.Name = nameof(TestClass.RetValueField) + "salt";
            var type = typeof(TestClass);
            mock.ReturnObject = TestClass.VALUE;
            mock.BeforeInjection(method);

            Assert.Throws<InitializationException>(() => mock.Initialize(type));
        }

        [Theory, AutoMoqData]
        internal void Initialize_RetValue_Success(
            [Frozen]Mock<IPrePostProcessor> preProc,
            [Frozen]FieldDefinition field,
            MethodDefinition method,
            ReplaceMock mock)
        {
            preProc.Setup(p => p.GenerateField(It.IsAny<string>(), It.IsAny<Type>())).Returns(field);
            field.Name = nameof(TestClass.RetValueField);
            var type = typeof(TestClass);
            mock.ReturnObject = TestClass.VALUE;
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
            [Frozen(Matching.ImplementedInterfaces)]SuccessfulArgumentChecker checker,
            ReplaceMock mock)
        {
            if (noReturnObject)
            {
                mock.ReturnObject = null;
                mock.ExpectedCalls = null;
            }

            mock.BeforeInjection(method);

            proc.Verify(m => m.GenerateField(It.IsAny<string>(),It.IsAny<Type>()),
                shouldBeInjected ? Times.AtLeastOnce() : Times.Never());
        }

        [Theory, AutoMoqData]
        internal void Initialize_ReturnInstance_Set(
            [Frozen]Mock<IPrePostProcessor> preProc,
            FieldDefinition field, MethodDefinition method,
            ReplaceMock mock)
        {
            field.Name = nameof(TestClass.RetValueField);
            preProc.Setup(p => p.GenerateField(It.IsAny<string>(), It.IsAny<Type>())).Returns((FieldDefinition)null);
            preProc.Setup(p => p.GenerateField(It.IsAny<string>(), It.IsAny<Type>())).Returns(field);
            mock.ReturnObject = 5;
            mock.ExpectedCalls = null;

            Assert.Null(TestClass.RetValueField);
            mock.BeforeInjection(method);
            mock.Initialize(typeof(TestClass));

            Assert.Equal(5 , TestClass.RetValueField);
            TestClass.RetValueField = null;
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
