using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoFake.Setup;
using AutoFixture.Xunit2;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Moq;
using Xunit;

namespace AutoFake.UnitTests.Setup
{
    public class SourceMethodTests
    {
        [Fact]
        public void Name_MethodInfo_ReturnsName()
        {
            var method = typeof(TestClass2).GetMethod(nameof(TestClass2.TestMethod));
            var sourceMethod = new SourceMethod(method);

            Assert.Equal(method.Name, sourceMethod.Name);
        }

        [Fact]
        public void ReturnType_MethodInfo_ReturnsType()
        {
            var method = typeof(TestClass2).GetMethod(nameof(TestClass2.TestMethod));
            var sourceMethod = new SourceMethod(method);

            Assert.Equal(method.ReturnType, sourceMethod.ReturnType);
        }

        [Fact]
        public void IsSourceInstruction_DifferentTypes_False()
        {
            var typeInfo = new TypeInfo(typeof(TestClass2), new List<FakeDependency>(), new FakeOptions());
            var method = typeInfo.GetMethods(m => m.Name == nameof(TestClass2.TestMethod)).Single();
            var instruction = Instruction.Create(OpCodes.Call, method);
            var sourceMember = new SourceMethod(GetMethod(nameof(TestClass.TestMethod), typeof(int)));

            Assert.False(sourceMember.IsSourceInstruction(typeInfo, instruction, new GenericArgument[0]));
        }

        [Fact]
        public void IsSourceInstruction_DifferentOverloads_False()
        {
            var typeInfo = new TypeInfo(typeof(TestClass), new List<FakeDependency>(), new FakeOptions());
            var method = typeInfo.GetMethods(m => m.Name == nameof(TestClass.TestMethod) && m.Parameters.Count == 0).Single();
            var instruction = Instruction.Create(OpCodes.Call, method);
            var sourceMember = new SourceMethod(GetMethod(nameof(TestClass.TestMethod), typeof(int)));

            Assert.False(sourceMember.IsSourceInstruction(typeInfo, instruction, new GenericArgument[0]));
        }

        [Fact]
        public void IsSourceInstruction_TheSameMethod_True()
        {
            var typeInfo = new TypeInfo(typeof(TestClass), new List<FakeDependency>(), new FakeOptions());
            var sourceMember = new SourceMethod(GetMethod(nameof(TestClass.TestMethod), typeof(int)));
            var method = typeInfo.GetMethods(m => m.Name == nameof(TestClass.TestMethod) && m.Parameters.Count == 1).Single();
            var instruction = Instruction.Create(OpCodes.Call, method);

            Assert.True(sourceMember.IsSourceInstruction(typeInfo, instruction, new GenericArgument[0]));
        }

        [Fact]
        public void GetParameters_MethodInfo_ReturnsParameters()
        {
            var method = typeof(TestClass2).GetMethod(nameof(TestClass2.TestMethod));
            var sourceMethod = new SourceMethod(method);

            Assert.Equal(method.GetParameters(), sourceMethod.GetParameters());
        }

        [Theory]
        [InlineData(nameof(TestClass2.TestMethod), true)]
        [InlineData(nameof(TestClass2.StaticTestMethod), false)]
        public void HasStackInstance_Field_Success(string methodName, bool expectedResult)
        {
            var method = typeof(TestClass2).GetMethod(methodName);
            var sourceMethod = new SourceMethod(method);

            Assert.Equal(expectedResult, sourceMethod.HasStackInstance);
        }

        [Fact]
        public void Equals_TheSameMethod_True()
        {
            var method = typeof(TestClass2).GetMethod(nameof(TestClass2.TestMethod));
            var sourceMethod1 = new SourceMethod(method);
            var sourceMethod2 = new SourceMethod(method);

            Assert.True(sourceMethod1.Equals(sourceMethod2));
        }

        [Fact]
        public void GetHashCode_Method_TheSameHashCodes()
        {
            var method = typeof(TestClass2).GetMethod(nameof(TestClass2.TestMethod));
            var sourceMethod = new SourceMethod(method);

            Assert.Equal(method.GetHashCode(), sourceMethod.GetHashCode());
        }

        [Fact]
        public void ToString_Method_TheSameStrings()
        {
            var method = typeof(TestClass2).GetMethod(nameof(TestClass2.TestMethod));
            var sourceMethod = new SourceMethod(method);

            Assert.Equal(method.ToString(), sourceMethod.ToString());
        }

        [Theory, AutoMoqData]
        public void Equals_ValidInput_True(Mock<MethodInfo> method, Mock<MethodInfo> method2)
        {
            method.Setup(m => m.Equals(method2.Object)).Returns(false);
            method2.Setup(m => m.Equals(method.Object)).Returns(false);

            var sut = new SourceMethod(method.Object);

            Assert.NotEqual(method.Object, method2.Object);
            Assert.False(sut.Equals(null));
            Assert.False(sut.Equals(new object()));
            Assert.False(sut.Equals(new SourceMethod(method2.Object)));
            Assert.True(sut.Equals(new SourceMethod(method.Object)));
        }

        private MethodInfo GetMethod(string methodName, params Type[] arguments) => GetMethod<TestClass>(methodName, arguments);
        private MethodInfo GetMethod<T>(string methodName, params Type[] arguments) => typeof(T).GetMethod(methodName, arguments);

        private class TestClass
        {
            public void TestMethod()
            {
            }

            public int TestMethod(int arg) => 5;
        }

        private class TestClass2
        {
            public int TestMethod(int arg) => 5;
            public static int StaticTestMethod(int arg) => 5;
        }
    }
}
