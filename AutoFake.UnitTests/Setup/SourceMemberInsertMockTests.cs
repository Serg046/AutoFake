using System;
using System.Collections.Generic;
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
        [InlineAutoMoqData(InsertMock.Location.Top, true)]
        [InlineAutoMoqData(InsertMock.Location.Bottom, false)]
        internal void Inject_MethodDescriptor_Injected(
            InsertMock.Location location,
            bool injectBeforeCmd,
            ClosureDescriptor descriptor,
            [Frozen]Mock<IProcessor> proc,
            [Frozen]IProcessorFactory factory)
        {
            var mock = new SourceMemberInsertMock(factory, Mock.Of<IInvocationExpression>(), descriptor, location);
            var cmd = Instruction.Create(OpCodes.Nop);

            mock.Inject(null, cmd);

            proc.Verify(m => m.InjectClosure(descriptor, injectBeforeCmd));
        }

        [Theory, AutoMoqData]
        internal void BeforeInjection_CapturedMembers_FieldsGenerated(
            [Frozen]Mock<IPrePostProcessor> prePostProcessor,
            IProcessorFactory processorFactory, IInvocationExpression expression,
            FieldDefinition field,
            MethodDefinition method)
        {
            var members = new List<CapturedMember>();
            members.Add(new CapturedMember(field, null, 5));
            var closure = new ClosureDescriptor("", "", members);
            var mock = new SourceMemberInsertMock(processorFactory, expression, closure, InsertMock.Location.Top);

            mock.BeforeInjection(method);

            prePostProcessor.Verify(p => p.GenerateField(It.Is<string>(s => s.EndsWith("Captured")), typeof(int)));
        }

        [Theory, AutoMoqData]
        internal void AfterInjection_NestedPrivateType_ChangedToNestedAssembly(
            [Frozen]ModuleDefinition module,
            IInvocationExpression expression,
            IProcessorFactory processorFactory)
        {
            var closure = new ClosureDescriptor("TestNs.TestClass", "", new CapturedMember[0]);
            var typeDef = new TypeDefinition("TestNs", "TestClass", TypeAttributes.NestedPrivate);
            module.Types.Add(typeDef);
            var mock = new SourceMemberInsertMock(processorFactory, expression, closure, InsertMock.Location.Top);

            mock.AfterInjection(null);

            Assert.Equal(TypeAttributes.NestedAssembly, typeDef.Attributes);
        }

        [Theory, AutoMoqData]
        internal void AfterInjection_NestedPublicType_NothingChanged(
            [Frozen]ModuleDefinition module,
            IInvocationExpression expression,
            IProcessorFactory processorFactory)
        {
            var closure = new ClosureDescriptor("TestNs.TestClass", "", new CapturedMember[0]);
            var typeDef = new TypeDefinition("TestNs", "TestClass", TypeAttributes.NestedPublic);
            module.Types.Add(typeDef);
            var mock = new SourceMemberInsertMock(processorFactory, expression, closure, InsertMock.Location.Top);

            mock.AfterInjection(null);

            Assert.Equal(TypeAttributes.NestedPublic, typeDef.Attributes);
        }

        [Fact]
        public void Initialize_GeneratedType_Nothing()
        {
            var mock = new SourceMemberInsertMock(Mock.Of<IProcessorFactory>(), Mock.Of<IInvocationExpression>(), null, InsertMock.Location.Top);

            var parameters = mock.Initialize(null);

            Assert.Empty(parameters);
        }

        [Theory, AutoMoqData]
        internal void Initialize_CapturedField_Success(
            [Frozen]Mock<IPrePostProcessor> prePostProc,
            IInvocationExpression expression,
            FieldDefinition field1, FieldDefinition field2,
            IProcessorFactory factory)
        {
            field1.Name = nameof(TestClass.Field);
            prePostProc
                .Setup(p => p.GenerateField(It.IsAny<string>(), It.IsAny<Type>()))
                .Returns(field1);
            var closure = new ClosureDescriptor("", "", new[] { new CapturedMember(field2, null, 5) });
            var mock = new SourceMemberInsertMock(factory, expression, closure, InsertMock.Location.Top);
            mock.BeforeInjection(null);

            mock.Initialize(typeof(TestClass));

            Assert.Equal(5, TestClass.Field);
            TestClass.Field = 0;
        }

        [Theory, AutoMoqData]
        internal void Initialize_IncorrectField_Fails(
            [Frozen]Mock<IPrePostProcessor> prePostProc,
            IInvocationExpression expression,
            FieldDefinition field1, FieldDefinition field2,
            IProcessorFactory factory)
        {
            field1.Name = nameof(TestClass.Field) + "salt";
            prePostProc
                .Setup(p => p.GenerateField(It.IsAny<string>(), It.IsAny<Type>()))
                .Returns(field1);
            var closure = new ClosureDescriptor("", "", new[] { new CapturedMember(field2, null, 5) });
            var mock = new SourceMemberInsertMock(factory, expression, closure, InsertMock.Location.Top);
            mock.BeforeInjection(null);

            Assert.Throws<InitializationException>(() => mock.Initialize(typeof(TestClass)));
        }

        private class TestClass
        {
            internal static int Field;
        }
    }
}
