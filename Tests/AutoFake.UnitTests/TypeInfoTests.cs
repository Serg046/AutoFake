using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using AutoFake.Exceptions;
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
			var options = new FakeOptions();
			var writer = new AssemblyWriter(new AssemblyReader(typeof(TestClass), options),
				new AssemblyHost(), options, new AssemblyPool());
			Assert.Throws<MissingMethodException>(() => writer.CreateFakeObject(new IMock[0],
				GetDependencies(new StringBuilder())));
			Assert.Throws<MissingMethodException>(() => writer.CreateFakeObject(new IMock[0],
				GetDependencies(new StringBuilder(), new StringBuilder(), new StringBuilder())));
			Assert.Throws<MissingMethodException>(() => writer.CreateFakeObject(new IMock[0],
                GetDependencies(1, 1)));
			writer.CreateFakeObject(new IMock[0], GetDependencies(new StringBuilder(), new StringBuilder()));
		}

		[Theory]
		[InlineData(typeof(InternalTestClass))]
		[InlineData(typeof(ProtectedTestClass))]
		[InlineData(typeof(PrivateTestClass))]
		[InlineData(typeof(GenericTestClass<int>))]
		public void CreateFakeObject_NonPublicConstructor_Success(Type type)
		{
			var options = new FakeOptions();
			var writer = new AssemblyWriter(new AssemblyReader(type, options),
				new AssemblyHost(), options, new AssemblyPool());
            var generated = writer.CreateFakeObject(new IMock[0], GetDependencies());

			generated.SourceType.FullName.Should().Be(type.FullName);
		}

		[Fact]
		public void CreateFakeObject_AmbiguousCtorAndNullAsDependency_ForcesToUseArgIsNull()
		{
			var options = new FakeOptions();
			var writer = new AssemblyWriter(new AssemblyReader(typeof(AmbiguousCtorTestClass), options),
				new AssemblyHost(), options, new AssemblyPool());

            Assert.Throws<InitializationException>(() => writer.CreateFakeObject(new IMock[0],
				GetDependencies(new object[] { null })));
            writer.CreateFakeObject(new IMock[0], new[] {Arg.IsNull<StreamReader>()});
            writer.CreateFakeObject(new IMock[0], new[] {Arg.IsNull<StreamWriter>()});
		}

		[Fact]
		public void AddField_Field_Added()
		{
			const string testName = "testName";
			var options = new FakeOptions();
			var writer = new AssemblyWriter(new AssemblyReader(GetType(), options),
				new AssemblyHost(), options, new AssemblyPool());

			writer.AddField(new FieldDefinition(testName, FieldAttributes.Assembly | FieldAttributes.Static,
				writer.ImportToSourceAsm(typeof(string))));

			var generated = writer.CreateFakeObject(new IMock[0], new object[0]);
			var field = generated.FieldsType.GetField(testName, BindingFlags.NonPublic | BindingFlags.Static);
			field.Should().NotBeNull();
			field.FieldType.Should().Be(typeof(string));
		}

		[Fact]
		public void AddField_ExistingField_AddedWithInc()
		{
			const string testName = "testName";
			var options = new FakeOptions();
			var writer = new AssemblyWriter(new AssemblyReader(GetType(), options),
				new AssemblyHost(), options, new AssemblyPool());

			writer.AddField(new FieldDefinition(testName, FieldAttributes.Assembly | FieldAttributes.Static,
				writer.ImportToSourceAsm(typeof(string))));
			writer.AddField(new FieldDefinition(testName, FieldAttributes.Assembly | FieldAttributes.Static,
				writer.ImportToSourceAsm(typeof(string))));

			var generated = writer.CreateFakeObject(new IMock[0], new object[0]);
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
	        var options = new FakeOptions();
	        var writer = new AssemblyWriter(new AssemblyReader(type, options),
		        new AssemblyHost(), options, new AssemblyPool());
            var mock = new Mock<IMock>();
            mock.Setup(m => m.Initialize(It.IsAny<Type>()));

            var fakeObjectInfo = writer.CreateFakeObject(new[] { mock.Object }, new object[0]);

            mock.Verify(m => m.Initialize(It.IsAny<Type>()));
            Assert.Equal(type.FullName, fakeObjectInfo.SourceType.FullName);
            var instanceAssertion = isStaticType ? (Action<object>)Assert.Null : Assert.NotNull;
            instanceAssertion(fakeObjectInfo.Instance);
        }

        [Fact]
        public void GetMethod_IsInBaseType_Returned()
        {
            var options = new FakeOptions();
            var assemblyReader = new AssemblyReader(typeof(TestClass), options);
            var assemblyPool = new AssemblyPool();
            var writer = new AssemblyWriter(assemblyReader, new AssemblyHost(), options, assemblyPool);
            var method = new MethodDefinition(nameof(BaseTestClass.BaseTypeMethod),
                MethodAttributes.Public, writer.ImportToSourceAsm(typeof(void)));

            var actualMethod = new TypeInfo(assemblyReader, options, assemblyPool).GetMethod(method, true);

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
        }

        internal class InternalTestClass
        {
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

        private object[] GetDependencies(params object[] args) => args;
    }
}
