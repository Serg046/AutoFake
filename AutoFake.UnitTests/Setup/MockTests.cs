using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoFake.Expression;
using AutoFake.Setup;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;

namespace AutoFake.UnitTests.Setu
{
    public class MockTests
    {
        [Fact]
        public void IsAsyncMethod_SyncMethod_False()
        {
            var typeInfo = new TypeInfo(typeof(TestClass), new List<FakeDependency>());
            var method = typeInfo.Methods.Single(m => m.Name == nameof(TestClass.TestMethod) && m.Parameters.Count == 0);
            var mock = new MockFake(GetMethod(nameof(TestClass.TestMethod)));

            MethodDefinition methodDefinition;
            Assert.False(mock.IsAsyncMethod(method, out methodDefinition));
        }

        [Fact]
        public void IsAsyncMethod_AsyncMethod_True()
        {
            var typeInfo = new TypeInfo(typeof(TestClass), new List<FakeDependency>());
            var method = typeInfo.Methods.Single(m => m.Name == nameof(TestClass.AsyncMethod));
            var mock = new MockFake(GetMethod(nameof(TestClass.TestMethod)));

            MethodDefinition methodDefinition;
            Assert.True(mock.IsAsyncMethod(method, out methodDefinition));
            Assert.NotNull(methodDefinition);
        }

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

        private class MockFake : Mock
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
            public override bool CheckArguments { get; }
            public override Func<byte, bool> ExpectedCalls { get; }

            public override void PrepareForInjecting(IMocker mocker) => throw new System.NotImplementedException();

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

            public async void AsyncMethod()
            {
            }
        }
    }
}
