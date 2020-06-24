using AutoFake.Setup.Mocks;
using AutoFixture.Xunit2;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Moq;
using System;
using AutoFake.Exceptions;
using Xunit;

namespace AutoFake.UnitTests.Setup
{
    public class InsertMockTests
    {
        [Theory]
        [InlineData(InsertMock.Location.Top, 1, 2, true)]
        [InlineData(InsertMock.Location.Bottom, 1, 2, false)]
        internal void IsSourceInstruction_Instruction_Success(InsertMock.Location location, int first, int last, bool shouldBeFirst)
        {
            var method = new MethodDefinition(null, MethodAttributes.Assembly, new FunctionPointerType());
            var firstInstruction = Instruction.Create(OpCodes.Ldc_I4, first);
            var lastInstruction = Instruction.Create(OpCodes.Ldc_I4, last);
            method.Body.Instructions.Add(firstInstruction);
            method.Body.Instructions.Add(lastInstruction);

            var mock = new InsertMock(Mock.Of<IProcessorFactory>(), null, location);

            Assert.True(mock.IsSourceInstruction(method, shouldBeFirst ? firstInstruction : lastInstruction));
            Assert.False(mock.IsSourceInstruction(method, shouldBeFirst ? lastInstruction : firstInstruction));
        }

        [Fact]
        public void IsSourceInstruction_UnexpectedLocation_False()
        {
            var mock = new InsertMock(Mock.Of<IProcessorFactory>(), null, (InsertMock.Location)(-1));

            Assert.False(mock.IsSourceInstruction(null, null));
        }

        [Theory, AutoMoqData]
        internal void Initialize_CapturedField_Success(
            [Frozen]Mock<IPrePostProcessor> proc,
            [Frozen]Action action,
            MethodDefinition method, FieldDefinition field1,
            InsertMock mock)
        {
            proc.Setup(p => p.GenerateField(It.IsAny<string>(), It.IsAny<Type>()))
                .Returns(field1);
            field1.Name = nameof(TestClass.Action);
            mock.BeforeInjection(method);

            mock.Initialize(typeof(TestClass));

            Assert.Equal(action, TestClass.Action);
            TestClass.Action = null;
        }

        [Theory, AutoMoqData]
        internal void Initialize_IncorrectField_Fails(
            [Frozen]Mock<IPrePostProcessor> proc,
            MethodDefinition method, FieldDefinition field1,
            InsertMock mock)
        {
            proc.Setup(p => p.GenerateField(It.IsAny<string>(), It.IsAny<Type>()))
                .Returns(field1);
            field1.Name = nameof(TestClass.Action) + "salt";
            mock.BeforeInjection(method);

            Assert.Throws<InitializationException>(() => mock.Initialize(typeof(TestClass)));
        }

        [Theory, AutoMoqData]
        internal void Inject_ValidInput_Success(
            [Frozen]Mock<IProcessorFactory> factory,
            [Frozen]Mock<IPrePostProcessor> preProc, [Frozen]Mock<IProcessor> proc,
            MethodDefinition method, FieldDefinition field,
            IEmitter emitter, Instruction instruction,
            InsertMock mock)
        {
            preProc.Setup(p => p.GenerateField(It.IsAny<string>(), typeof(Action))).Returns(field);
            mock.BeforeInjection(method);

            mock.Inject(emitter, instruction);

            factory.Verify(f => f.CreateProcessor(emitter, instruction));
            proc.Verify(p => p.InjectClosure(field, InsertMock.Location.Top));
        }

        private class TestClass
        {
            internal static Action Action;
        }
    }
}
