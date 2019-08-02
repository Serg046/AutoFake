using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using AutoFake.Setup;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Moq;
using Xunit;

namespace AutoFake.UnitTests
{
    public class FakeGeneratorTests
    {
        private readonly FakeGenerator _fakeGenerator;

        public FakeGeneratorTests()
        {
            var typeInfo = new TypeInfo(typeof(TestClass), new List<FakeDependency>());
            _fakeGenerator = new FakeGenerator(typeInfo, Moq.Mock.Of<MockerFactory>());
        }

        [Fact]
        public void Generate_Mock_PreparedForInjecting()
        {
            var testMethod = GetMethodInfo(nameof(TestClass.SimpleMethod));
            var mock = new Mock<IMock>();

            _fakeGenerator.Generate(new[] { mock.Object }, new List<MockedMemberInfo>(), testMethod);

            mock.Verify(m => m.BeforeInjection(It.IsAny<IMocker>()));
        }

        [Fact]
        public void Generate_MultipleMocks_SuffixUpdated()
        {
            var simpleMethodName = nameof(TestClass.SimpleMethod);
            var testMethod = GetMethodInfo(simpleMethodName);
            var mock = new Mock<IMock>();
            var mockedMembers = new List<MockedMemberInfo>();

            _fakeGenerator.Generate(new[] { mock.Object, mock.Object, mock.Object }, mockedMembers, testMethod);

            Assert.Equal(3, mockedMembers.Count);
            Assert.EndsWith(simpleMethodName, mockedMembers[0].UniqueName);
            Assert.EndsWith(simpleMethodName + "1", mockedMembers[1].UniqueName);
            Assert.EndsWith(simpleMethodName + "2", mockedMembers[2].UniqueName);
        }

        [Fact]
        public void ApplyMock_IsInstalledInstruction_Injected()
        {
            var testMethod = GetMethodInfo(nameof(TestClass.TestMethod));
            var innerMethod = GetMethodInfo(nameof(TestClass.SimpleMethod));
            var mock = new Mock<IMock>();
            var mockedMembers = new List<MockedMemberInfo>();

            _fakeGenerator.Generate(new[] { mock.Object }, mockedMembers, testMethod);

            mock.Verify(m => m.IsSourceInstruction(It.IsAny<ITypeInfo>(),
                It.Is<Instruction>(cmd => Equivalent(cmd.Operand, innerMethod))));
        }

        [Fact]
        public void Generate_IsAsyncMethod_RecursivelyCallsWithBody()
        {
            var asyncMethod = GetMethodInfo(nameof(TestClass.AsyncMethod));
            var innerMethod = GetMethodInfo(nameof(TestClass.SimpleMethod));
            var mock = new Mock<IMock>();
            var mockedMembers = new List<MockedMemberInfo>();

            _fakeGenerator.Generate(new[] { mock.Object }, mockedMembers, asyncMethod);

            mock.Verify(m => m.IsSourceInstruction(It.IsAny<ITypeInfo>(),
                It.Is<Instruction>(cmd => Equivalent(cmd.Operand, innerMethod))));
        }

        private bool Equivalent(object operand, MethodInfo innerMethod)
        {
            return operand is MethodReference method && method.EquivalentTo(innerMethod);
        }

        public static IEnumerable<object[]> GetCallbackFieldTestData()
        {
            yield return new object[] {null, false};
            yield return new object[] {new Action(() => Console.WriteLine(0)), true};
        }

        private MethodInfo GetMethodInfo(string name) => typeof(TestClass).GetMethod(name);

        private class TestClass
        {
            public void SimpleMethod()
            {
                var a = 5;
                var b = a;
            }

            public void TestMethod()
            {
                SimpleMethod();
            }

            public async void AsyncMethod()
            {
                await Task.Delay(1);
                SimpleMethod();
            }
        }
    }
}
