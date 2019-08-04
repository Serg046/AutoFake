using System;
using System.Reflection;
using AutoFake.Expression;
using AutoFake.Setup;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;

namespace AutoFake.UnitTests.Setup
{
    public class MockTests
    {
        [Fact]
        public void Initialize_SetupBodyField_ExpressionSet()
        {
            var mock = new MockFake(GetMethod(nameof(TestClass.TestMethod)));
            var mockedMember = new MockedMemberInfo(mock, null);
            mockedMember.SetupBodyField = new FieldDefinition(nameof(TestClass.InvocationExpression),
                Mono.Cecil.FieldAttributes.Assembly, new FunctionPointerType());

            Assert.Null(TestClass.InvocationExpression);
            mock.Initialize(mockedMember, typeof(TestClass));

            Assert.Equal(mock.InvocationExpression, TestClass.InvocationExpression);
            TestClass.InvocationExpression = null;
        }

        private MethodInfo GetMethod(string methodName, params Type[] arguments) => GetMethod<TestClass>(methodName, arguments);
        private MethodInfo GetMethod<T>(string methodName, params Type[] arguments) => typeof(T).GetMethod(methodName, arguments);

        private class MockFake : SourceMemberMock
        {
            public MockFake(MethodInfo method) : this(Moq.Mock.Of<IInvocationExpression>(
                m => m.GetSourceMember() == new SourceMethod(method)))
            {
            }

            private MockFake(IInvocationExpression invocationExpression) : base(invocationExpression)
            {
                InvocationExpression = invocationExpression;
            }

            public IInvocationExpression InvocationExpression { get; }

            public override void AfterInjection(IMocker mocker, ILProcessor ilProcessor) => throw new NotImplementedException();

            public override void BeforeInjection(IMocker mocker) => throw new System.NotImplementedException();

            public override void Inject(IMethodMocker methodMocker, ILProcessor ilProcessor, Instruction instruction)
                => throw new NotImplementedException();
        }

        private class TestClass
        {
            internal static IInvocationExpression InvocationExpression;

            public void TestMethod()
            {
            }

            public int TestMethod(int arg) => 5;
        }
    }
}
