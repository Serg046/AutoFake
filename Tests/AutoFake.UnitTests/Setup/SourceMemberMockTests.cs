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
    public class SourceMemberMockTests
    {
        [Theory, AutoMoqData]
        internal void Initialize_SetupBodyField_ExpressionSet(
            [Frozen]IInvocationExpression expression,
            [Frozen]FieldDefinition field,
            MethodDefinition method,
            Mock mock)
        {
            field.Name = nameof(TestClass.InvocationExpression);
            mock.ExpectedCalls = null;
            mock.BeforeInjection(method);

            Assert.Null(TestClass.InvocationExpression);
            mock.Initialize(typeof(TestClass));

            Assert.Equal(expression, TestClass.InvocationExpression);
            TestClass.InvocationExpression = null;
        }

        [Theory, AutoMoqData]
        internal void Initialize_IncorrectSetupBodyField_Fails(
            [Frozen]FieldDefinition field,
            MethodDefinition method,
            Mock mock)
        {
            field.Name = nameof(TestClass.InvocationExpression) + "salt";
            mock.BeforeInjection(method);

            Assert.Throws<InitializationException>(() => mock.Initialize(typeof(TestClass)));
        }

        [Theory, AutoMoqData]
        internal void Initialize_NoSetupBodyField_NoEffect(Mock mock)
        {
            mock.ExpectedCalls = null;

            Assert.Null(TestClass.InvocationExpression);
            mock.Initialize(typeof(TestClass));

            Assert.Null(TestClass.InvocationExpression);
        }

        [Theory, AutoMoqData]
        internal void Initialize_ExpectedCallsField_ExpressionSet(
            [Frozen]Mock<IPrePostProcessor> proc,
            FieldDefinition field,
            MethodDefinition method,
            Mock mock)
        {
            field.Name = nameof(TestClass.CallsCounter);
            proc.Setup(p => p.GenerateField(It.IsAny<string>(), It.IsAny<Type>())).Returns(field);
            proc.Setup(p => p.GenerateField(It.IsAny<string>(), typeof(IInvocationExpression))).Returns((FieldDefinition)null);
            mock.ExpectedCalls = b => true;
            mock.BeforeInjection(method);

            Assert.Null(TestClass.CallsCounter);
            mock.Initialize(typeof(TestClass));

            Assert.Equal(mock.ExpectedCalls, TestClass.CallsCounter);
            TestClass.CallsCounter = null;
        }

        [Theory, AutoMoqData]
        internal void Initialize_IncorrectExpectedCallsField_Fails(
            [Frozen]Mock<IPrePostProcessor> proc,
            FieldDefinition field,
            MethodDefinition method,
            Mock mock)
        {
            field.Name = nameof(TestClass.CallsCounter) + "salt";
            proc.Setup(p => p.GenerateField(It.IsAny<string>(), It.IsAny<Type>())).Returns(field);
            proc.Setup(p => p.GenerateField(It.IsAny<string>(), typeof(IInvocationExpression))).Returns((FieldDefinition)null);
            mock.ExpectedCalls = b => true;
            mock.BeforeInjection(method);
            mock.BeforeInjection(method);

            Assert.Throws<InitializationException>(() => mock.Initialize(typeof(TestClass)));
        }

        [Theory, AutoMoqData]
        internal void Initialize_NoExpectedCallsField_NoEffect(
            [Frozen]Mock<IPrePostProcessor> proc,
            Mock mock)
        {
            mock.ExpectedCalls = null;
            proc.Setup(p => p.GenerateField(It.IsAny<string>(), typeof(IInvocationExpression))).Returns((FieldDefinition)null);

            Assert.Null(TestClass.CallsCounter);
            mock.Initialize(typeof(TestClass));

            Assert.Null(TestClass.CallsCounter);
        }

        [Theory]
        [InlineAutoMoqData(false, false, false)]
        [InlineAutoMoqData(false, true, true)]
        [InlineAutoMoqData(true, false, true)]
        [InlineAutoMoqData(true, true, true)]
        internal void BeforeInjection_NeedCheckArgumentsOrExpectedCallsCount_Injected(
            bool needCheckArguments, bool expectedCallsCount, bool shouldBeInjected,
            [Frozen]Mock<IPrePostProcessor> preProc,
            MethodDefinition method,
            IProcessorFactory processorFactory,
            Mock<IInvocationExpression> expression)
        {
            expression.Setup(e => e.GetArguments()).Returns(new List<IFakeArgument>
            {
                new FakeArgument(needCheckArguments
                    ? new EqualityArgumentChecker(1)
                    : (IFakeArgumentChecker)new SuccessfulArgumentChecker())
            });
            var mock = new VerifyMock(processorFactory, expression.Object);
            mock.ExpectedCalls = expectedCallsCount ? new Func<byte, bool>(i => true) : null;

            mock.BeforeInjection(method);

            var times = shouldBeInjected ? Times.AtLeastOnce() : Times.Never();
            preProc.Verify(m => m.GenerateField(It.IsAny<string>(), It.IsAny<Type>()), times);
            preProc.Verify(m => m.GenerateCallsAccumulator(It.IsAny<string>(), It.IsAny<MethodBody>()), times);
        }

        [Theory, AutoMoqData]
        internal void IsSourceInstruction_Cmd_CallsSourceMember(
            [Frozen]Mock<ISourceMember> member,
            Mock mock)
        {
            var cmd = Instruction.Create(OpCodes.Nop);

            mock.IsSourceInstruction(null, cmd);

            member.Verify(s => s.IsSourceInstruction(It.IsAny<ITypeInfo>(), cmd));
        }

        [Theory]
        [InlineAutoMoqData(false, false, false)]
        [InlineAutoMoqData(true, false, true)]
        [InlineAutoMoqData(false, true, true)]
        [InlineAutoMoqData(true, true, true)]
        internal void AfterInjection_Flags_VerificationInjected(
            bool checkArgs, bool expectedCalls, bool injected,
            [Frozen]ModuleDefinition module,
            [Frozen]Mock<IPrePostProcessor> postProc,
            IProcessorFactory processorFactory,
            Mock<IInvocationExpression> expression,
            TypeDefinition type,
            IEmitter emitter)
        {
            expression.Setup(e => e.GetArguments()).Returns(new List<IFakeArgument>
            {
                new FakeArgument(checkArgs
                    ? new EqualityArgumentChecker(1)
                    : (IFakeArgumentChecker)new SuccessfulArgumentChecker())
            });
            var mock = new Mock(processorFactory, expression.Object);
            module.Types.Add(type);
            mock.ExpectedCalls = expectedCalls ? new Func<byte, bool>(i => true) : null;

            mock.AfterInjection(emitter);

            postProc.Verify(m => m.InjectVerification(emitter, checkArgs, It.IsAny<FieldDefinition>(),
                    It.IsAny<FieldDefinition>(), It.IsAny<FieldDefinition>()),
                injected ? Times.Once() : Times.Never());
        }

        [Theory, AutoMoqData]
        internal void CheckArguments_SuccessfulCheckers_False(
            [Frozen(Matching.ImplementedInterfaces)]SuccessfulArgumentChecker checker,
            SourceMemberMock mock)
        {
            Assert.False(mock.CheckArguments);
        }

        [Theory, AutoMoqData]
        internal void CheckArguments_EqualityArgumentCheckers_True(
            [Frozen(Matching.ImplementedInterfaces)]EqualityArgumentChecker checker,
            SourceMemberMock mock)
        {
            Assert.True(mock.CheckArguments);
        }

        internal class Mock: SourceMemberMock
        {
            public Mock(IProcessorFactory processorFactory, IInvocationExpression invocationExpression) : base(processorFactory, invocationExpression)
            {
            }

            public override void Inject(IEmitter emitter, Instruction instruction)
            {
                throw new NotImplementedException();
            }
        }

        private class TestClass
        {
            public static IInvocationExpression InvocationExpression;
            public static Func<byte, bool> CallsCounter;
        }
    }
}
