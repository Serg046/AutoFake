using AutoFake.Setup.Mocks;
using AutoFixture.Xunit2;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Moq;
using System;
using System.Collections.Generic;
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
        internal void Inject_Instruction_Injected([Frozen]Mock<IProcessor> proc, InsertMock sut)
        {
            var cmd = Instruction.Create(OpCodes.Nop);

            sut.Inject(null, cmd);

            proc.Verify(m => m.InjectClosure(sut.Closure, true));
        }

        [Fact]
        public void Initialize_GeneratedType_Nothing()
        {
            var mock = new InsertMock(Mock.Of<IProcessorFactory>(), null, InsertMock.Location.Top);

            var parameters = mock.Initialize(null);

            Assert.Empty(parameters);
        }

        [Theory, AutoMoqData]
        internal void Initialize_CapturedField_Success(
            [Frozen]Mock<IPrePostProcessor> prePostProc,
            FieldDefinition field1, FieldDefinition field2,
            IProcessorFactory factory)
        {
            field1.Name = nameof(TestClass.Field);
            prePostProc
                .Setup(p => p.GenerateField(It.IsAny<string>(), It.IsAny<Type>()))
                .Returns(field1);
            var closure = new ClosureDescriptor("", "", new[] { new CapturedMember(field2, null, 5) });
            var mock = new InsertMock(factory, closure, InsertMock.Location.Top);
            mock.BeforeInjection(null);

            mock.Initialize(typeof(TestClass));

            Assert.Equal(5, TestClass.Field);
            TestClass.Field = 0;
        }

        [Theory, AutoMoqData]
        internal void Initialize_IncorrectField_Fails(
            [Frozen]Mock<IPrePostProcessor> prePostProc,
            FieldDefinition field1, FieldDefinition field2,
            IProcessorFactory factory)
        {
            field1.Name = nameof(TestClass.Field) + "salt";
            prePostProc
                .Setup(p => p.GenerateField(It.IsAny<string>(), It.IsAny<Type>()))
                .Returns(field1);
            var closure = new ClosureDescriptor("", "", new[] { new CapturedMember(field2, null, 5) });
            var mock = new InsertMock(factory, closure, InsertMock.Location.Top);
            mock.BeforeInjection(null);

            Assert.Throws<InitializationException>(() => mock.Initialize(typeof(TestClass)));
        }

        [Theory, AutoMoqData]
        internal void AfterInjection_NestedPrivateType_ChangedToNestedAssembly(
            [Frozen]ModuleDefinition module,
            IProcessorFactory processorFactory)
        {
            var closure = new ClosureDescriptor("TestNs.TestClass", "", new CapturedMember[0]);
            var typeDef = new TypeDefinition("TestNs", "TestClass", TypeAttributes.NestedPrivate);
            module.Types.Add(typeDef);
            var mock = new InsertMock(processorFactory, closure, InsertMock.Location.Top);

            mock.AfterInjection(null);

            Assert.Equal(TypeAttributes.NestedAssembly, typeDef.Attributes);
        }

        [Theory, AutoMoqData]
        internal void AfterInjection_NestedPublicType_NothingChanged(
            [Frozen]ModuleDefinition module,
            IProcessorFactory processorFactory)
        {
            var closure = new ClosureDescriptor("TestNs.TestClass", "", new CapturedMember[0]);
            var typeDef = new TypeDefinition("TestNs", "TestClass", TypeAttributes.NestedPublic);
            module.Types.Add(typeDef);
            var mock = new InsertMock(processorFactory, closure, InsertMock.Location.Top);

            mock.AfterInjection(null);

            Assert.Equal(TypeAttributes.NestedPublic, typeDef.Attributes);
        }

        private class TestClass
        {
            internal static int Field;
        }
    }
}
