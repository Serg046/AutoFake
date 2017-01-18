using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoFake.Setup;
using Mono.Cecil.Cil;
using Xunit;

namespace AutoFake.UnitTests.Setup
{
    public class SourceConstructorTests
    {
        [Fact]
        public void Name_ConstructorInfo_ReturnsName()
        {
            var ctor = typeof(TestClass2).GetConstructors().Single();
            var sourceMember = new SourceConstructor(ctor);

            Assert.Equal(ctor.Name, sourceMember.Name);
        }

        [Fact]
        public void ReturnType_ConstructorInfo_ReturnsType()
        {
            var ctor = typeof(TestClass2).GetConstructors().Single();
            var sourceMember = new SourceConstructor(ctor);

            Assert.Equal(ctor.DeclaringType, sourceMember.ReturnType);
        }

        [Fact]
        public void IsCorrectInstruction_DifferentTypes_False()
        {
            var typeInfo = new TypeInfo(typeof(TestClass2), new List<FakeDependency>());
            var method = typeInfo.Methods.Single(m => m.Name == ".ctor");
            var instruction = Instruction.Create(OpCodes.Call, method);
            var sourceMember = new SourceConstructor(GetCtor(typeof(int)));

            Assert.False(sourceMember.IsCorrectInstruction(typeInfo, instruction));
        }

        [Fact]
        public void IsCorrectInstruction_DifferentOverloads_False()
        {
            var typeInfo = new TypeInfo(typeof(TestClass), new List<FakeDependency>());
            var ctor = typeInfo.Methods.Single(m => m.Name == ".ctor" && m.Parameters.Count == 0);
            var instruction = Instruction.Create(OpCodes.Call, ctor);
            var sourceMember = new SourceConstructor(GetCtor(typeof(int)));

            Assert.False(sourceMember.IsCorrectInstruction(typeInfo, instruction));
        }

        [Fact]
        public void IsCorrectInstruction_TheSameMethod_True()
        {
            var typeInfo = new TypeInfo(typeof(TestClass), new List<FakeDependency>());
            var sourceMember = new SourceConstructor(GetCtor(typeof(int)));
            var method = typeInfo.Methods.Single(m => m.Name == ".ctor" && m.Parameters.Count == 1);
            var instruction = Instruction.Create(OpCodes.Call, method);

            Assert.True(sourceMember.IsCorrectInstruction(typeInfo, instruction));
        }

        [Fact]
        public void GetParameters_ConstructorInfo_ReturnsParameters()
        {
            var ctor = typeof(TestClass2).GetConstructors().Single();
            var sourceMember = new SourceConstructor(ctor);

            Assert.Equal(ctor.GetParameters(), sourceMember.GetParameters());
        }

        [Fact]
        public void HasStackInstance_AnyCtor_ReturnsFalse()
        {
            var ctor = typeof(TestClass).GetConstructor(BindingFlags.CreateInstance | BindingFlags.Public
                                                        | BindingFlags.Instance, null, new Type[0], null);
            var sourceMember = new SourceConstructor(ctor);

            Assert.False(sourceMember.HasStackInstance);

        }

        private ConstructorInfo GetCtor(params Type[] arguments) => GetCtor<TestClass>(arguments);
        private ConstructorInfo GetCtor<T>(params Type[] arguments) => typeof(T).GetConstructor(arguments);

        private class TestClass
        {
            public TestClass()
            {
            }

            public TestClass(int arg)
            {
            }
        }

        private class TestClass2
        {
            public TestClass2(int arg)
            {
            }
        }
    }
}
