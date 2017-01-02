using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoFake.Setup;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;

namespace AutoFake.UnitTests.Setup.MockTests
{
    public class MockTests
    {
        [Fact]
        public void IsInstalledMethod_DifferentTypes_False()
        {
            var typeInfo = new TypeInfo(typeof(TestClass2), new List<FakeDependency>());
            var mock = new MockFake(GetMethod(nameof(TestClass.TestMethod), typeof(int)));
            var method = typeInfo.Methods.Single(m => m.Name == nameof(TestClass2.TestMethod));

            Assert.False(mock.IsInstalledMethod(typeInfo, method));
        }

        [Fact]
        public void IsInstalledMethod_DifferentOverloads_False()
        {
            var typeInfo = new TypeInfo(typeof(TestClass), new List<FakeDependency>());
            var mock = new MockFake(GetMethod(nameof(TestClass.TestMethod), typeof(int)));
            var method = typeInfo.Methods.Single(m => m.Name == nameof(TestClass.TestMethod) && m.Parameters.Count == 0);

            Assert.False(mock.IsInstalledMethod(typeInfo, method));
        }

        [Fact]
        public void IsInstalledMethod_TheSameMethod_True()
        {
            var typeInfo = new TypeInfo(typeof(TestClass), new List<FakeDependency>());
            var mock = new MockFake(GetMethod(nameof(TestClass.TestMethod), typeof(int)));
            var method = typeInfo.Methods.Single(m => m.Name == nameof(TestClass.TestMethod) && m.Parameters.Count == 1);

            Assert.True(mock.IsInstalledMethod(typeInfo, method));
        }

        [Fact]
        public void IsMethodInstruction_IncorrectInstruction_False()
        {
            var mock = new MockFake(null);

            Assert.False(mock.IsMethodInstruction(Instruction.Create(OpCodes.Nop)));
        }

        [Fact]
        public void IsMethodInstruction_CorrectInstruction_True()
        {
            var mock = new MockFake(null);
            var method = GetMethod(nameof(TestClass.TestMethod));
            var typeInfo = new TypeInfo(typeof(TestClass), new List<FakeDependency>());

            Assert.True(mock.IsMethodInstruction(Instruction.Create(OpCodes.Call, typeInfo.Import(method))));
        }

        [Fact]
        public void IsAsyncMethod_SyncMethod_False()
        {
            var typeInfo = new TypeInfo(typeof(TestClass), new List<FakeDependency>());
            var method = typeInfo.Methods.Single(m => m.Name == nameof(TestClass.TestMethod) && m.Parameters.Count == 0);
            var mock = new MockFake(null);

            MethodDefinition methodDefinition;
            Assert.False(mock.IsAsyncMethod(method, out methodDefinition));
        }

        [Fact]
        public void IsAsyncMethod_AsyncMethod_True()
        {
            var typeInfo = new TypeInfo(typeof(TestClass), new List<FakeDependency>());
            var method = typeInfo.Methods.Single(m => m.Name == nameof(TestClass.AsyncMethod));
            var mock = new MockFake(null);

            MethodDefinition methodDefinition;
            Assert.True(mock.IsAsyncMethod(method, out methodDefinition));
            Assert.NotNull(methodDefinition);
        }

        private MethodInfo GetMethod(string methodName, params Type[] arguments) => GetMethod<TestClass>(methodName, arguments);
        private MethodInfo GetMethod<T>(string methodName, params Type[] arguments) => typeof(T).GetMethod(methodName, arguments);

        private class MockFake : Mock
        {
            public MockFake(MethodInfo method) : base(method, null)
            {
            }

            public override void PrepareForInjecting(IMocker mocker)
            {
                throw new System.NotImplementedException();
            }

            public override void Inject(IMethodMocker methodMocker, ILProcessor ilProcessor, Instruction instruction)
            {
                throw new System.NotImplementedException();
            }

            public override void Initialize(MockedMemberInfo mockedMemberInfo, GeneratedObject generatedObject)
            {
                throw new System.NotImplementedException();
            }

            public override void Verify(MockedMemberInfo mockedMemberInfo, GeneratedObject generatedObject)
            {
                throw new System.NotImplementedException();
            }
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

        private class TestClass2
        {
            public int TestMethod(int arg) => 5;
        }
    }
}
