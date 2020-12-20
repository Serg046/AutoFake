using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using AutoFake.Exceptions;
using AutoFake.Setup;
using AutoFake.Setup.Mocks;
using FluentAssertions;
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
		public void CreateFakeObject_InvalidDependencies_Throws()
		{
			var type = typeof(TestClass);
			Assert.Throws<InitializationException>(() => new TypeInfo(type, 
				GetDependencies(new StringBuilder())).CreateFakeObject(new MockCollection(), new FakeOptions()));
			Assert.Throws<InitializationException>(() => new TypeInfo(type, GetDependencies(new StringBuilder(),
				new StringBuilder(), new StringBuilder())).CreateFakeObject(new MockCollection(), new FakeOptions()));
			Assert.Throws<InitializationException>(() => new TypeInfo(type, 
				GetDependencies(1, 1)).CreateFakeObject(new MockCollection(), new FakeOptions()));
			new TypeInfo(type, GetDependencies(new StringBuilder(), new StringBuilder()))
				.CreateFakeObject(new MockCollection(), new FakeOptions());
		}

		[Theory]
		[InlineData(typeof(InternalTestClass))]
		[InlineData(typeof(ProtectedTestClass))]
		[InlineData(typeof(PrivateTestClass))]
		[InlineData(typeof(GenericTestClass<int>))]
		public void CreateFakeObject_NonPublicConstructor_Success(Type type)
		{
			var generated = new TypeInfo(type, GetDependencies())
				.CreateFakeObject(new MockCollection(), new FakeOptions());

			generated.SourceType.FullName.Should().Be(type.FullName);
		}

		[Fact]
		public void CreateFakeObject_AmbiguousCtorAndNullAsDependency_ForcesToUseArgIsNull()
		{
			var type = typeof(AmbiguousCtorTestClass);

			Assert.Throws<InitializationException>(() => new TypeInfo(type, 
				GetDependencies(new object[] { null })).CreateFakeObject(new MockCollection(), new FakeOptions()));
			new TypeInfo(type, new[] { Arg.IsNull<StreamReader>() })
				.CreateFakeObject(new MockCollection(), new FakeOptions());
			new TypeInfo(type, new[] { Arg.IsNull<StreamWriter>() })
				.CreateFakeObject(new MockCollection(), new FakeOptions());
			new TypeInfo(typeof(TestClass), GetDependencies(null, null))
				.CreateFakeObject(new MockCollection(), new FakeOptions());
		}

		[Fact]
		public void AddField_Field_Added()
		{
			const string testName = "testName";
			var typeInfo = new TypeInfo(GetType(), new List<FakeDependency>());

			typeInfo.AddField(new FieldDefinition(testName, FieldAttributes.Assembly | FieldAttributes.Static,
				typeInfo.ImportReference(typeof(string))));

			var generated = typeInfo.CreateFakeObject(new MockCollection(), new FakeOptions());
			var field = generated.FieldsType.GetField(testName, BindingFlags.NonPublic | BindingFlags.Static);
			field.Should().NotBeNull();
			field.FieldType.Should().Be(typeof(string));
		}

		[Fact]
		public void AddField_ExistingField_AddedWithInc()
		{
			const string testName = "testName";
			var typeInfo = new TypeInfo(GetType(), new List<FakeDependency>());

			typeInfo.AddField(new FieldDefinition(testName, FieldAttributes.Assembly | FieldAttributes.Static,
				typeInfo.ImportReference(typeof(string))));
			typeInfo.AddField(new FieldDefinition(testName, FieldAttributes.Assembly | FieldAttributes.Static,
				typeInfo.ImportReference(typeof(string))));

			var generated = typeInfo.CreateFakeObject(new MockCollection(), new FakeOptions());
			var field1 = generated.FieldsType.GetField(testName, BindingFlags.NonPublic | BindingFlags.Static);
			field1.Should().NotBeNull();
			field1.FieldType.Should().Be(typeof(string));
			var field2 = generated.FieldsType.GetField(testName + "1", BindingFlags.NonPublic | BindingFlags.Static);
			field2.Should().NotBeNull();
			field2.FieldType.Should().Be(typeof(string));
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

            var fakeObjectInfo = typeInfo.CreateFakeObject(mocks, new FakeOptions());

            mock.Verify(m => m.Initialize(It.IsAny<Type>()));
            Assert.Equal(type.FullName, fakeObjectInfo.SourceType.FullName);
            var instanceAssertion = isStaticType ? (Action<object>)Assert.Null : Assert.NotNull;
            instanceAssertion(fakeObjectInfo.Instance);
        }

        [Fact]
        public void GetMethod_IsInBaseType_Returned()
        {
            var type = new TypeInfo(typeof(TestClass), new List<FakeDependency>());
            var method = new MethodDefinition(nameof(BaseTestClass.BaseTypeMethod),
                MethodAttributes.Public, type.ImportReference(typeof(void)));

            var actualMethod = type.GetMethod(method);

            Assert.Equal(method.ReturnType.ToString(), actualMethod.ReturnType.ToString());
            Assert.Equal(method.Name, actualMethod.Name);
        }

        [Fact]
        public void GetDerivedVirtualMethods()
        {
	        var type = new TypeInfo(typeof(BaseTestClass), new List<FakeDependency>());
	        var method = type.GetMethods(m => m.Name == nameof(BaseTestClass.VirtualMethod)).Single();

	        var overridenMethods = type.GetDerivedVirtualMethods(method);

	        overridenMethods.Should().HaveCount(1);
	        overridenMethods.Single().DeclaringType.Name.Should().Be(nameof(TestClass));
        }

        private class TestClass : BaseTestClass
        {
            public TestClass(StringBuilder dependency1, StringBuilder dependency2)
            {
            }

            public override void VirtualMethod() => base.VirtualMethod();
        }

        private class BaseTestClass
        {
            public void BaseTypeMethod() { }

            public virtual void VirtualMethod() { }
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

        private class GenericTestClass<T>
        {
	        private GenericTestClass()
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
