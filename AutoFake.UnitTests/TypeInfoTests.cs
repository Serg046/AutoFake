using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AutoFake.Exceptions;
using Mono.Cecil;
using Xunit;

namespace AutoFake.UnitTests
{
    public class TypeInfoTests
    {
        [Fact]
        public void CreateInstance_InvalidDependencies_Throws()
        {
            var type = typeof(TestClass);
            Assert.Throws<FakeGeneretingException>(
                () => new TypeInfo(type, GetDependencies(new StringBuilder())).CreateInstance(type));
            Assert.Throws<FakeGeneretingException>(
                () => new TypeInfo(type, GetDependencies(new StringBuilder(), new StringBuilder(), new StringBuilder())).CreateInstance(type));
            Assert.Throws<FakeGeneretingException>(
                () => new TypeInfo(type, GetDependencies(1, 1)).CreateInstance(type));
            new TypeInfo(type, GetDependencies(new StringBuilder(), new StringBuilder())).CreateInstance(type);
        }

        [Theory]
        [InlineData(typeof(InternalTestClass))]
        [InlineData(typeof(ProtectedTestClass))]
        [InlineData(typeof(PrivateTestClass))]
        public void CreateInstance_NonPublicConstructor_Success(Type type)
        {
            new TypeInfo(type, GetDependencies()).CreateInstance(type);
        }

        [Fact]
        public void CreateInstance_AmbiguousCtorAndNullAsDependency_ForcesToUseArgIsNull()
        {
            var type = typeof(AmbiguousCtorTestClass);

            Assert.Throws<FakeGeneretingException>(
                () => new TypeInfo(type, GetDependencies(new object[] {null})).CreateInstance(type));
            new TypeInfo(type, new[] {Arg.IsNull<StreamReader>()}).CreateInstance(type);
            new TypeInfo(type, new[] {Arg.IsNull<StreamWriter>()}).CreateInstance(type);

            new TypeInfo(typeof(TestClass), GetDependencies(null, null)).CreateInstance(typeof(TestClass));
        }

        [Fact]
        public void GetMonoCecilTypeName_SystemType_ReturnsSourceTypeName()
        {
            var sourceType = typeof(InternalTestClass);
            var typeInfo = new TypeInfo(sourceType, new List<FakeDependency>());

            Assert.Equal("System.Object", typeInfo.GetMonoCecilTypeName(typeof(object)));
        }

        [Fact]
        public void GetMonoCecilTypeName_NestedType_ReturnsSourceTypeName()
        {
            var typeInfo = new TypeInfo(typeof(InternalTestClass), new List<FakeDependency>());

            Assert.Equal("AutoFake.UnitTests.TypeInfoTests/TestClass",
                typeInfo.GetMonoCecilTypeName(typeof(TestClass)));
        }

        [Fact]
        public void Module_SomeType_TheSameModulePaths()
        {
            var sourceType = typeof(TestClass);
            var typeInfo = new TypeInfo(sourceType, new List<FakeDependency>());

            Assert.Equal(sourceType.Module.FullyQualifiedName, typeInfo.Module.FullyQualifiedName);
        }

        [Fact]
        public void AddField_Field_Added()
        {
            const string testName = "testName";
            var typeInfo = new TypeInfo(GetType(), new List<FakeDependency>());

            typeInfo.AddField(new FieldDefinition(testName, FieldAttributes.Assembly, new FunctionPointerType()));

            Assert.NotNull(typeInfo.Fields.Single(f => f.Name == testName));
        }

        [Fact]
        public void AddMethod_Method_Added()
        {
            const string testName = "testName";
            var typeInfo = new TypeInfo(GetType(), new List<FakeDependency>());

            typeInfo.AddMethod(new MethodDefinition(testName, MethodAttributes.Assembly, new FunctionPointerType()));

            Assert.NotNull(typeInfo.Methods.Single(f => f.Name == testName));
        }

        private class TestClass
        {
            public TestClass(StringBuilder dependency1, StringBuilder dependency2)
            {
            }
        }

        internal class InternalTestClass
        {
            internal InternalTestClass()
            {
            }
        }

        protected class ProtectedTestClass
        {
            protected ProtectedTestClass()
            {
            }
        }

        private class PrivateTestClass
        {
            private PrivateTestClass()
            {
            }
        }

        private class AmbiguousCtorTestClass
        {
            public AmbiguousCtorTestClass(StreamReader reader)
            {
            }

            public AmbiguousCtorTestClass(StreamWriter writer)
            {
            }
        }

        private IList<FakeDependency> GetDependencies(params object[] args)
            => args.Select(a => new FakeDependency(a?.GetType(), a)).ToList();
    }
}
