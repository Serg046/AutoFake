using System;
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
            Assert.Null(TestClass.InvocationExpression);
            mock.Initialize(typeof(TestClass));

            Assert.Null(TestClass.InvocationExpression);
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
            Mock mock)
        {
            mock.CheckArguments = needCheckArguments;
            if (!expectedCallsCount) mock.ExpectedCalls = null;

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
            TypeDefinition type,
            IEmitter emitter,
            Mock mock)
        {
            module.Types.Add(type);
            mock.CheckArguments = checkArgs;
            mock.ExpectedCalls = expectedCalls ? new MethodDescriptor(type.FullName, string.Empty) : null;

            mock.AfterInjection(emitter);

            postProc.Verify(m => m.InjectVerification(emitter, checkArgs, mock.ExpectedCalls,
                    It.IsAny<FieldDefinition>(), It.IsAny<FieldDefinition>()),
                injected ? Times.Once() : Times.Never());
        }

        [Theory, AutoMoqData]
        internal void AfterInjection_NestedPrivateType_ChangedToAssemblyAccess(
            [Frozen]ModuleDefinition module,
            TypeDefinition type,
            IEmitter emitter,
            Mock mock)
        {
            type.Attributes = TypeAttributes.NestedPrivate;
            module.Types.Add(type);

            mock.ExpectedCalls = new MethodDescriptor(type.FullName, string.Empty);

            mock.AfterInjection(emitter);

            Assert.Equal(TypeAttributes.NestedAssembly, type.Attributes);
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
            internal static IInvocationExpression InvocationExpression;
        }
    }
}
