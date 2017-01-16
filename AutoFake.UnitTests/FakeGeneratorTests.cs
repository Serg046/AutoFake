using System;
using System.Collections.Generic;
using System.Reflection;
using AutoFake.Exceptions;
using AutoFake.Setup;
using Mono.Cecil.Cil;
using Moq;
using Xunit;

namespace AutoFake.UnitTests
{
    public class FakeGeneratorTests
    {
        private readonly GeneratedObject _generatedObject;
        private readonly FakeGenerator _fakeGenerator;

        public FakeGeneratorTests()
        {
            var typeInfo = new TypeInfo(typeof(TestClass), new List<FakeDependency>());
            var mocker = new Mock<IMocker>();
            var mockerFactory = new Mock<MockerFactory>();
            mockerFactory.Setup(m => m.CreateMocker(typeInfo, It.IsAny<MockedMemberInfo>())).Returns(mocker.Object);
            _generatedObject = new GeneratedObject(typeInfo);

            _fakeGenerator = new FakeGenerator(typeInfo, mockerFactory.Object, _generatedObject);
        }

        [Fact]
        public void Generate_BuiltObject_Throws()
        {
            _generatedObject.Build();
            Assert.Throws<FakeGeneretingException>(() => _fakeGenerator.Generate(null, null));
        }

        [Fact]
        public void Generate_NoInvocations_ProcessIsNotCalled()
        {
            var mock = new ReplaceableMockFake(typeof(DateTime).GetProperty(nameof(DateTime.Now)).GetMethod, new List<FakeArgument>(),
                new ReplaceableMock.Parameters());
            var testMethod = GetMethodInfo(nameof(TestClass.SimpleMethod));

            //Assert
            _fakeGenerator.Generate(new[] {mock}, testMethod);
        }

        [Fact]
        public void Generate_MethodWithOneInvocation_ProcessOnce()
        {
            var mock = new ReplaceableMockFake(typeof(DateTime).GetProperty(nameof(DateTime.Now)).GetMethod, new List<FakeArgument>(),
                new ReplaceableMock.Parameters());
            var testMethod = GetMethodInfo(nameof(TestClass.GetDateNow));

            Assert.Throws<InjectInvocationException>(() => _fakeGenerator.Generate(new[] {mock}, testMethod));
        }

        [Fact]
        public void Generate_ValidInput_AnalyzesOnlyClientCode()
        {
            var mock = new ReplaceableMockFake(typeof(DateTime).GetProperty(nameof(DateTime.UtcNow)).GetMethod, new List<FakeArgument>(),
                new ReplaceableMock.Parameters());
            var testMethod = GetMethodInfo(nameof(TestClass.GetDateNow));

            //Assert
            _fakeGenerator.Generate(new[] { mock }, testMethod);
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

            public void GetDateNow()
            {
                var a = DateTime.Now;
            }
        }

        private class ReplaceableMockFake : ReplaceableMock
        {
            public ReplaceableMockFake(MethodInfo method, List<FakeArgument> setupArguments, Parameters parameters)
                : base(new SourceMethod(method), setupArguments, parameters)
            {
            }

            public override void Inject(IMethodMocker methodMocker, ILProcessor ilProcessor, Instruction instruction)
            {
                throw new InjectInvocationException();
            }
        }

        private class InjectInvocationException : Exception
        {
        }
    }
}
