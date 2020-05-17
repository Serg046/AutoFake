using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using AutoFake.Setup.Mocks;
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
            _fakeGenerator = new FakeGenerator(typeInfo);
        }

        [Fact]
        public void Generate_Mock_PreparedForInjecting()
        {
            var testMethod = GetMethodInfo(nameof(TestClass.SimpleMethod));
            var mock = new Mock<IMock>();

            _fakeGenerator.Generate(new[] { mock.Object }, testMethod);

            mock.Verify(m => m.BeforeInjection(It.Is<MethodDefinition>(met => met.Name == testMethod.Name)));
        }

        [Fact]
        public void Generate_IsInstalledInstruction_Injected()
        {
            var testMethod = GetMethodInfo(nameof(TestClass.TestMethod));
            var innerMethod = GetMethodInfo(nameof(TestClass.SimpleMethod));
            var mock = new Mock<IMock>();
            mock.Setup(m => m.IsSourceInstruction(It.IsAny<MethodDefinition>(),
                It.Is<Instruction>(cmd => Equivalent(cmd.Operand, innerMethod)))).Returns(true);

            _fakeGenerator.Generate(new[] { mock.Object }, testMethod);

            mock.Verify(m => m.Inject(It.IsAny<IEmitter>(), It.Is<Instruction>(cmd => Equivalent(cmd.Operand, innerMethod))));
        }

        [Fact]
        public void Generate_IsAsyncMethod_RecursivelyCallsWithBody()
        {
            var asyncMethod = GetMethodInfo(nameof(TestClass.AsyncMethod));
            var innerMethod = GetMethodInfo(nameof(TestClass.SimpleMethod));
            var mock = new Mock<IMock>();

            _fakeGenerator.Generate(new[] { mock.Object }, asyncMethod);

            mock.Verify(m => m.IsSourceInstruction(It.IsAny<MethodDefinition>(),
                It.Is<Instruction>(cmd => Equivalent(cmd.Operand, innerMethod))));
        }

        private bool Equivalent(object operand, MethodInfo innerMethod) => 
            operand is MethodReference method &&
            method.Name == innerMethod.Name &&
            method.Parameters.Count == innerMethod.GetParameters().Length;

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
