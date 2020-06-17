using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AutoFake.Exceptions;
using AutoFake.Setup;
using AutoFake.Setup.Mocks;
using Mono.Cecil;
using Moq;
using Xunit;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;

namespace AutoFake.UnitTests
{
    public class TypeInfoTests
    {
        [Fact]
        public void CreateInstance_InvalidDependencies_Throws()
        {
            var type = typeof(TestClass);
            Assert.Throws<InitializationException>(
                () => new TypeInfo(type, GetDependencies(new StringBuilder())).CreateInstance(type));
            Assert.Throws<InitializationException>(
                () => new TypeInfo(type, GetDependencies(new StringBuilder(), new StringBuilder(), new StringBuilder())).CreateInstance(type));
            Assert.Throws<InitializationException>(
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

            Assert.Throws<InitializationException>(
                () => new TypeInfo(type, GetDependencies(new object[] {null})).CreateInstance(type));
            new TypeInfo(type, new[] {Arg.IsNull<StreamReader>()}).CreateInstance(type);
            new TypeInfo(type, new[] {Arg.IsNull<StreamWriter>()}).CreateInstance(type);

            new TypeInfo(typeof(TestClass), GetDependencies(null, null)).CreateInstance(typeof(TestClass));
        }

        [Fact]
        public void Module_SomeType_TheSameModulePaths()
        {
            var sourceType = typeof(TestClass);
            var typeInfo = new TypeInfo(sourceType, new List<FakeDependency>());

            Assert.Equal(sourceType.Module.FullyQualifiedName, typeInfo.Module.FileName);
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
        public void AddField_ExistingField_AddedWithInc()
        {
            const string testName = "testName";
            var typeInfo = new TypeInfo(GetType(), new List<FakeDependency>());

            typeInfo.AddField(new FieldDefinition(testName, FieldAttributes.Assembly, new FunctionPointerType()));
            typeInfo.AddField(new FieldDefinition(testName, FieldAttributes.Assembly, new FunctionPointerType()));

            Assert.Equal(new[] { testName, testName + "1" }, typeInfo.Fields.Select(f => f.Name));
        }

        [Fact]
        public void AddMethod_Method_Added()
        {
            const string testName = "testName";
            var typeInfo = new TypeInfo(GetType(), new List<FakeDependency>());

            typeInfo.AddMethod(new MethodDefinition(testName, MethodAttributes.Assembly, new FunctionPointerType()));

            Assert.NotNull(typeInfo.Methods.Single(f => f.Name == testName));
        }

        [Theory]
        [InlineData(typeof(InternalTestClass), false)]
        [InlineData(typeof(StaticTestClass), true)]
        public void CreateFakeObject_Type_Created(Type type, bool isStaticType)
        {
            var typeInfo = new TypeInfo(type, new FakeDependency[0]);
            var mock = new Mock<IMock>();
            mock.Setup(m => m.Initialize(It.IsAny<Type>())).Returns(new List<object>());
            var mocks = new MockCollection();
            mocks.Add(type.GetMethod("CreateFakeObjectTypeCreated"), new[] {mock.Object});

            var fakeObjectInfo = typeInfo.CreateFakeObject(mocks);

            mock.Verify(m => m.Initialize(It.IsAny<Type>()));
            Assert.Equal(type.FullName, fakeObjectInfo.Type.FullName);
            var instanceAssertion = isStaticType ? (Action<object>)Assert.Null : Assert.NotNull;
            instanceAssertion(fakeObjectInfo.Instance);
        }

        [Fact]
        public void GetMethod_IsInBaseType_Returned()
        {
            var type = new TypeInfo(typeof(TestClass), new List<FakeDependency>());
            var method = new MethodDefinition(nameof(BaseTestClass.BaseTypeMethod),
                MethodAttributes.Public, type.Module.ImportReference(typeof(void)));

            var actualMethod = type.GetMethod(method);

            Assert.Equal(method.ReturnType.ToString(), actualMethod.ReturnType.ToString());
            Assert.Equal(method.Name, actualMethod.Name);
        }

        private class TestClass : BaseTestClass
        {
            public TestClass(StringBuilder dependency1, StringBuilder dependency2)
            {
            }
        }

        private class BaseTestClass
        {
            public void BaseTypeMethod() { }
        }

        private static class StaticTestClass
        {
            public static void CreateFakeObjectTypeCreated() { }
        }

        internal class InternalTestClass
        {
            public void CreateFakeObjectTypeCreated() { }
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
