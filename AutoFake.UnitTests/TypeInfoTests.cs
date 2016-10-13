using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AutoFake.Exceptions;
using GuardExtensions;
using Xunit;

namespace AutoFake.UnitTests
{
    public class TypeInfoTests
    {
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
            => args.Select(a => FakeDependency.Create(a?.GetType(), a)).ToList();

        [Fact]
        public void Ctor_Null_Throws()
        {
            Assert.Throws<ContractFailedException>(() => new TypeInfo(typeof(TestClass), null));
            Assert.Throws<ContractFailedException>(() => new TypeInfo(null, GetDependencies()));
            new TypeInfo(typeof(TestClass), GetDependencies());
        }

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
        public void CreateInstance_AmbiguousCtorAndNullAsDependency_ForcesToUseFakeDependency()
        {
            var type = typeof(AmbiguousCtorTestClass);

            Assert.Throws<FakeGeneretingException>(
                () => new TypeInfo(type, GetDependencies(new object[] {null})).CreateInstance(type));
            new TypeInfo(type, new[] {FakeDependency.Null<StreamReader>()}).CreateInstance(type);
            new TypeInfo(type, new[] {FakeDependency.Null<StreamWriter>()}).CreateInstance(type);

            new TypeInfo(typeof(TestClass), GetDependencies(null, null)).CreateInstance(typeof(TestClass));
        }
    }
}
