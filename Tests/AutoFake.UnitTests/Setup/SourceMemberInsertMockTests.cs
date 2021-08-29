using System;
using AutoFake.Exceptions;
using AutoFake.Expression;
using AutoFake.Setup.Mocks;
using AutoFixture.Xunit2;
using Mono.Cecil;
using Moq;
using Xunit;

namespace AutoFake.UnitTests.Setup
{
    public class SourceMemberInsertMockTests
    {
	    [Theory, AutoMoqData]
        internal void Initialize_CapturedField_Success(
            [Frozen]Mock<IPrePostProcessor> proc,
            [Frozen]Action action,
            MethodDefinition method,
            FieldDefinition body, FieldDefinition field1, FieldDefinition ctxField,
            SourceMemberInsertMock mock)
        {
	        body.Name = nameof(TestClass.SetupBody);
	        ctxField.Name = nameof(TestClass.ExecutionContext);
            proc.Setup(p => p.GenerateField(It.IsAny<string>(), It.IsAny<Type>())).Returns(field1);
	        proc.Setup(p => p.GenerateField(It.IsAny<string>(), typeof(IInvocationExpression))).Returns(body);
	        proc.Setup(p => p.GenerateField(It.IsAny<string>(), typeof(ExecutionContext))).Returns(ctxField);
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
            public static IInvocationExpression SetupBody;
            public static ExecutionContext ExecutionContext;
            public static Action Action;
        }
    }
}
