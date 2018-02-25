﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoFake.Setup;
using Mono.Cecil.Cil;
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
        public void IsCorrectInstruction_DifferentTypes_False()
        {
            var typeInfo = new TypeInfo(typeof(TestClass2), new List<FakeDependency>());
            var method = typeInfo.Methods.Single(m => m.Name == nameof(TestClass2.TestMethod));
            var instruction = Instruction.Create(OpCodes.Call, method);
            var sourceMember = new SourceMethod(GetMethod(nameof(TestClass.TestMethod), typeof(int)));

            Assert.False(sourceMember.IsCorrectInstruction(typeInfo, instruction));
        }

        [Fact]
        public void IsCorrectInstruction_DifferentOverloads_False()
        {
            var typeInfo = new TypeInfo(typeof(TestClass), new List<FakeDependency>());
            var method = typeInfo.Methods.Single(m => m.Name == nameof(TestClass.TestMethod) && m.Parameters.Count == 0);
            var instruction = Instruction.Create(OpCodes.Call, method);
            var sourceMember = new SourceMethod(GetMethod(nameof(TestClass.TestMethod), typeof(int)));

            Assert.False(sourceMember.IsCorrectInstruction(typeInfo, instruction));
        }

        [Fact]
        public void IsCorrectInstruction_TheSameMethod_True()
        {
            var typeInfo = new TypeInfo(typeof(TestClass), new List<FakeDependency>());
            var sourceMember = new SourceMethod(GetMethod(nameof(TestClass.TestMethod), typeof(int)));
            var method = typeInfo.Methods.Single(m => m.Name == nameof(TestClass.TestMethod) && m.Parameters.Count == 1);
            var instruction = Instruction.Create(OpCodes.Call, method);

            Assert.True(sourceMember.IsCorrectInstruction(typeInfo, instruction));
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