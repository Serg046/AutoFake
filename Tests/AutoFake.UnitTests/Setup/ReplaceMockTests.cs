using System;
using AutoFake.Exceptions;
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
        [Theory, AutoMoqData]
        internal void Initialize_NoRetValueField_NoEffect(
            [Frozen]Mock<IPrePostProcessor> preProc,
            MethodDefinition method,
            FieldDefinition ctx,
            ReplaceMock mock)
        {
	        ctx.Name = nameof(TestClass.ExecutionContext);
            preProc.Setup(p => p.GenerateField(It.IsAny<string>(), It.IsAny<Type>())).Returns((FieldDefinition)null);
            preProc.Setup(p => p.GenerateField(It.IsAny<string>(), typeof(ExecutionContext))).Returns(ctx);
            mock.ReturnObject = null;
            mock.ExpectedCalls = null;

            Assert.Null(TestClass.RetValueField);
            mock.BeforeInjection(method);
            mock.Initialize(typeof(TestClass));

            Assert.Null(TestClass.RetValueField);
        }

        [Theory, AutoMoqData]
        internal void Initialize_IncorrectRetValueField_Fails(
            [Frozen]Mock<IPrePostProcessor> preProc,
            [Frozen]FieldDefinition field,
            FieldDefinition ctx,
            MethodDefinition method,
            ReplaceMock mock)
        {
	        ctx.Name = nameof(TestClass.ExecutionContext);
            preProc.Setup(p => p.GenerateField(It.IsAny<string>(), It.IsAny<Type>())).Returns((FieldDefinition)null);
            preProc.Setup(p => p.GenerateField(It.IsAny<string>(), mock.SourceMember.ReturnType)).Returns(field);
            preProc.Setup(p => p.GenerateField(It.IsAny<string>(), typeof(ExecutionContext))).Returns(ctx);
            field.Name = nameof(TestClass.RetValueField) + "salt";
            var type = typeof(TestClass);
            mock.ReturnObject = TestClass.VALUE;
            mock.ExpectedCalls = null;

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
            }

            mock.BeforeInjection(method);

            proc.Verify(m => m.GenerateField(It.Is<string>(name => name.EndsWith("RetValue")), It.IsAny<Type>()),
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
            public static object RetValueField;
            public static ExecutionContext ExecutionContext;

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
