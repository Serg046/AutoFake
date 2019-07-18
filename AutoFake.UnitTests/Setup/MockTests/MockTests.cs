using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoFake.Expression;
using AutoFake.Setup;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;

namespace AutoFake.UnitTests.Setup.MockTests
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

        private MethodInfo GetMethod(string methodName, params Type[] arguments) => GetMethod<TestClass>(methodName, arguments);
        private MethodInfo GetMethod<T>(string methodName, params Type[] arguments) => typeof(T).GetMethod(methodName, arguments);

        private class MockFake : Mock
        {
            public MockFake(MethodInfo method) : base(Moq.Mock.Of<IInvocationExpression>(
                m => m.GetSourceMember() == new SourceMethod(method)))
            {
            }

            public override bool CheckArguments { get; }
            public override Func<byte, bool> ExpectedCalls { get; }

            public override void PrepareForInjecting(IMocker mocker) => throw new System.NotImplementedException();

            public override void Inject(IMethodMocker methodMocker, ILProcessor ilProcessor, Instruction instruction)
                => throw new NotImplementedException();

            public override IList<object> Initialize(MockedMemberInfo mockedMemberInfo, Type type) => throw new System.NotImplementedException();
        }

        private class TestClass
        {
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
