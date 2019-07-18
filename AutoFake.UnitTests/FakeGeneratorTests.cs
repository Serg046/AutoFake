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
            var mockedMembers = new Mock<MockedMemberInfo>();

            _fakeGenerator.Generate(new[] { mock.Object }, new List<MockedMemberInfo>(), testMethod);

            mock.Verify(m => m.PrepareForInjecting(It.IsAny<IMocker>()));
        }

        [Fact]
        public void Generate_MultipleMocks_SuffixUpdated()
        {
            var simpleMethodName = nameof(TestClass.SimpleMethod);
            var testMethod = GetMethodInfo(simpleMethodName);
            var mock = new Mock<IMock>();
            mock.Setup(m => m.SourceMember).Returns(new SourceMethod(testMethod));
            var mockedMembers = new List<MockedMemberInfo>();

            _fakeGenerator.Generate(new[] { mock.Object, mock.Object, mock.Object }, mockedMembers, testMethod);

            Assert.Equal(3, mockedMembers.Count);
            Assert.EndsWith(simpleMethodName, mockedMembers[0].GenerateFieldName());
            Assert.EndsWith(simpleMethodName + "1", mockedMembers[1].GenerateFieldName());
            Assert.EndsWith(simpleMethodName + "2", mockedMembers[2].GenerateFieldName());
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
