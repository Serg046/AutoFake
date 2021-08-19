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
				GetDependencies(new StringBuilder()), new FakeOptions())
				.CreateFakeObject(new MockCollection()));
			Assert.Throws<InitializationException>(() => new TypeInfo(type, 
				GetDependencies(new StringBuilder(), new StringBuilder(), new StringBuilder()),
				new FakeOptions()).CreateFakeObject(new MockCollection()));
			Assert.Throws<InitializationException>(() => new TypeInfo(type, 
				GetDependencies(1, 1), new FakeOptions()).CreateFakeObject(new MockCollection()));
			new TypeInfo(type, GetDependencies(new StringBuilder(), new StringBuilder()),
				new FakeOptions()).CreateFakeObject(new MockCollection());
		}

		[Theory]
		[InlineData(typeof(InternalTestClass))]
		[InlineData(typeof(ProtectedTestClass))]
		[InlineData(typeof(PrivateTestClass))]
		[InlineData(typeof(GenericTestClass<int>))]
		public void CreateFakeObject_NonPublicConstructor_Success(Type type)
		{
			var generated = new TypeInfo(type, GetDependencies(), new FakeOptions())
				.CreateFakeObject(new MockCollection());

			generated.SourceType.FullName.Should().Be(type.FullName);
		}

		[Fact]
		public void CreateFakeObject_AmbiguousCtorAndNullAsDependency_ForcesToUseArgIsNull()
		{
			var type = typeof(AmbiguousCtorTestClass);

			Assert.Throws<InitializationException>(() => new TypeInfo(type, 
				GetDependencies(new object[] { null }), new FakeOptions())
				.CreateFakeObject(new MockCollection()));
			new TypeInfo(type, new[] { Arg.IsNull<StreamReader>() }, new FakeOptions())
				.CreateFakeObject(new MockCollection());
			new TypeInfo(type, new[] { Arg.IsNull<StreamWriter>() }, new FakeOptions())
				.CreateFakeObject(new MockCollection());
			new TypeInfo(typeof(TestClass), GetDependencies(null, null), new FakeOptions())
				.CreateFakeObject(new MockCollection());
		}

		[Fact]
		public void AddField_Field_Added()
		{
			const string testName = "testName";
			var typeInfo = new TypeInfo(GetType(), new List<FakeDependency>(), new FakeOptions());

			typeInfo.AddField(new FieldDefinition(testName, FieldAttributes.Assembly | FieldAttributes.Static,
				typeInfo.ImportReference(typeof(string))));

			var generated = typeInfo.CreateFakeObject(new MockCollection());
			var field = generated.FieldsType.GetField(testName, BindingFlags.NonPublic | BindingFlags.Static);
			field.Should().NotBeNull();
			field.FieldType.Should().Be(typeof(string));
		}

		[Fact]
		public void AddField_ExistingField_AddedWithInc()
		{
			const string testName = "testName";
			var typeInfo = new TypeInfo(GetType(), new List<FakeDependency>(), new FakeOptions());

			typeInfo.AddField(new FieldDefinition(testName, FieldAttributes.Assembly | FieldAttributes.Static,
				typeInfo.ImportReference(typeof(string))));
			typeInfo.AddField(new FieldDefinition(testName, FieldAttributes.Assembly | FieldAttributes.Static,
				typeInfo.ImportReference(typeof(string))));

			var generated = typeInfo.CreateFakeObject(new MockCollection());
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
            var typeInfo = new TypeInfo(type, new FakeDependency[0], new FakeOptions());
            var mock = new Mock<IMock>();
            mock.Setup(m => m.Initialize(It.IsAny<Type>())).Returns(new List<object>());
            var mocks = new MockCollection();
            mocks.Add(type.GetMethod("CreateFakeObjectTypeCreated"), new[] {mock.Object});

            var fakeObjectInfo = typeInfo.CreateFakeObject(mocks);

            mock.Verify(m => m.Initialize(It.IsAny<Type>()));
            Assert.Equal(type.FullName, fakeObjectInfo.SourceType.FullName);
            var instanceAssertion = isStaticType ? (Action<object>)Assert.Null : Assert.NotNull;
            instanceAssertion(fakeObjectInfo.Instance);
        }

        [Fact]
        public void GetMethod_IsInBaseType_Returned()
        {
            var type = new TypeInfo(typeof(TestClass), new List<FakeDependency>(), new FakeOptions());
            var method = new MethodDefinition(nameof(BaseTestClass.BaseTypeMethod),
                MethodAttributes.Public, type.ImportReference(typeof(void)));

            var actualMethod = type.GetMethod(method, true);

            Assert.Equal(method.ReturnType.ToString(), actualMethod.ReturnType.ToString());
            Assert.Equal(method.Name, actualMethod.Name);
        }

        private class TestClass : BaseTestClass
        {
            public TestClass(StringBuilder dependency1, StringBuilder dependency2)
            {
            }

            public override void VirtualMethod() => base.VirtualMethod();

            public override string ToString() => "overriden";
        }

        private class BaseTestClass
        {
            public void BaseTypeMethod() { }

            public virtual void VirtualMethod() { }

            public override string ToString() => "base";
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
