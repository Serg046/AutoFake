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
            [Frozen] Mock<IPrePostProcessor> proc,
            [Frozen]IInvocationExpression expression,
            FieldDefinition setupField, FieldDefinition ctxField,
            MethodDefinition method,
            Mock mock)
        {
	        setupField.Name = nameof(TestClass.InvocationExpression);
	        ctxField.Name = nameof(TestClass.ExecutionContext);
	        proc.Setup(p => p.GenerateField(It.IsAny<string>(), typeof(IInvocationExpression))).Returns(setupField);
	        proc.Setup(p => p.GenerateField(It.IsAny<string>(), typeof(ExecutionContext))).Returns(ctxField);
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
        internal void Initialize_NoSetupBodyField_Fails(
            [Frozen] Mock<IPrePostProcessor> proc,
            FieldDefinition ctxField,
            MethodDefinition method,
	        Mock mock)
        {
	        ctxField.Name = nameof(TestClass.ExecutionContext);
	        proc.Setup(p => p.GenerateField(It.IsAny<string>(), typeof(IInvocationExpression))).Returns((FieldDefinition)null);
	        proc.Setup(p => p.GenerateField(It.IsAny<string>(), typeof(ExecutionContext))).Returns(ctxField);
            mock.BeforeInjection(method);
            mock.ExpectedCalls = null;

            Assert.Throws<InvalidOperationException>(() => mock.Initialize(typeof(TestClass)));
        }

        [Theory, AutoMoqData]
        internal void BeforeInjection_Method_Injected(
            [Frozen]Mock<IPrePostProcessor> preProc,
            MethodDefinition method,
            IProcessorFactory processorFactory,
            Mock<IInvocationExpression> expression)
        {
            expression.Setup(e => e.GetArguments()).Returns(new List<IFakeArgument>
            {
                new FakeArgument(new SuccessfulArgumentChecker())
            });
            var mock = new VerifyMock(processorFactory, expression.Object) {ExpectedCalls = i => true};

            mock.BeforeInjection(method);

            preProc.Verify(m => m.GenerateField(It.IsAny<string>(), It.IsAny<Type>()));
        }

        [Theory, AutoMoqData]
        internal void IsSourceInstruction_Cmd_CallsSourceMember(
            [Frozen]Mock<ISourceMember> member,
            Mock mock)
        {
            var cmd = Instruction.Create(OpCodes.Nop);

            mock.IsSourceInstruction(null, cmd, new GenericArgument[0]);

            member.Verify(s => s.IsSourceInstruction(It.IsAny<IAssemblyWriter>(), cmd, It.IsAny<IEnumerable<GenericArgument>>()));
        }

        [Theory, AutoMoqData]
        internal void AfterInjection_InjectsVerification(
            [Frozen] Mock<IPrePostProcessor> proc,
            FieldDefinition setupField, MethodDefinition method,
            IEmitter emitter, 
	        SourceMemberMock mock)
		{
	        setupField.Name = nameof(TestClass.InvocationExpression);
	        proc.Setup(p => p.GenerateField(It.IsAny<string>(), typeof(IInvocationExpression))).Returns(setupField);
            mock.BeforeInjection(method);

	        mock.AfterInjection(emitter);

            proc.Verify(p => p.InjectVerification(emitter, It.IsAny<FieldDefinition>(), It.IsAny<FieldDefinition>()));
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
            public static ExecutionContext ExecutionContext;
            public static Func<uint, bool> CallsCounter;
        }
    }
}
