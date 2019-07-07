using System;
using System.Collections.Generic;
using System.Reflection;
using AutoFake.Exceptions;
using AutoFake.Setup;
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
            _generatedObject = new GeneratedObject(typeInfo);
            _fakeGenerator = new FakeGenerator(typeInfo, Moq.Mock.Of<MockerFactory>(), _generatedObject);
        }

        [Fact]
        public void Generate_BuiltObject_Throws()
        {
            _generatedObject.Build();
            Assert.Throws<FakeGeneretingException>(() => _fakeGenerator.Generate(null, null));
        }

        [Fact]
        public void Generate_Mock_PreparedForInjecting()
        {
            var testMethod = GetMethodInfo(nameof(TestClass.SimpleMethod));
            var mock = new Mock<IMock>();

            _fakeGenerator.Generate(new[] {mock.Object}, testMethod);

            mock.Verify(m => m.PrepareForInjecting(It.IsAny<IMocker>()));
        }

        [Fact]
        public void Generate_MultipleMocks_SuffixUpdated()
        {
            var simpleMethodName = nameof(TestClass.SimpleMethod);
            var testMethod = GetMethodInfo(simpleMethodName);
            var mock = new Mock<IMock>();
            mock.Setup(m => m.SourceMember).Returns(new SourceMethod(testMethod));

            _fakeGenerator.Generate(new[] { mock.Object, mock.Object, mock.Object }, testMethod);

            Assert.Equal(3, _generatedObject.MockedMembers.Count);
            Assert.EndsWith(simpleMethodName, _generatedObject.MockedMembers[0].GenerateFieldName());
            Assert.EndsWith(simpleMethodName + "1", _generatedObject.MockedMembers[1].GenerateFieldName());
            Assert.EndsWith(simpleMethodName + "2", _generatedObject.MockedMembers[2].GenerateFieldName());
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
        }
    }
}
