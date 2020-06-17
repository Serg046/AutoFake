using System;
using AutoFake.Exceptions;
using AutoFake.Expression;
using AutoFake.Setup.Mocks;
using AutoFixture.Xunit2;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Moq;
using Xunit;

namespace AutoFake.UnitTests.Setup
{
    public class SourceMemberInsertMockTests
    {
        [Theory]
        [InlineAutoMoqData(InsertMock.Location.Top)]
        [InlineAutoMoqData(InsertMock.Location.Bottom)]
        internal void Inject_Action_Injected(
            InsertMock.Location location,
            Action descriptor,
            IInvocationExpression expression,
            [Frozen]Mock<IProcessor> proc,
            [Frozen]IProcessorFactory factory)
        {
            var mock = new SourceMemberInsertMock(factory, expression, descriptor, location);
            var cmd = Instruction.Create(OpCodes.Nop);

            mock.Inject(null, cmd);

            proc.Verify(m => m.InjectClosure(It.IsAny<FieldDefinition>(), location));
        }

        [Theory, AutoMoqData]
        internal void Initialize_CapturedField_Success(
            [Frozen]Mock<IPrePostProcessor> proc,
            [Frozen]Action action,
            [Frozen(Matching.ImplementedInterfaces)]SuccessfulArgumentChecker checker,
            MethodDefinition method, FieldDefinition field1,
            SourceMemberInsertMock mock)
        {
            proc.Setup(p => p.GenerateField(It.IsAny<string>(), It.IsAny<Type>()))
                .Returns(field1);
            field1.Name = nameof(TestClass.Action);
            mock.ExpectedCalls = null;
            mock.BeforeInjection(method);

            mock.Initialize(typeof(TestClass));

            Assert.Equal(action, TestClass.Action);
            TestClass.Action = null;
        }

        [Theory, AutoMoqData]
        internal void Initialize_IncorrectField_Fails(
            [Frozen]Mock<IPrePostProcessor> proc,
            MethodDefinition method, FieldDefinition field1,
            SourceMemberInsertMock mock)
        {
            proc.Setup(p => p.GenerateField(It.IsAny<string>(), It.IsAny<Type>()))
                .Returns(field1);
            field1.Name = nameof(TestClass.Action) + "salt";
            mock.ExpectedCalls = null;
            mock.BeforeInjection(method);

            Assert.Throws<InitializationException>(() => mock.Initialize(typeof(TestClass)));
        }

        private class TestClass
        {
            internal static Action Action;
        }
    }
}
