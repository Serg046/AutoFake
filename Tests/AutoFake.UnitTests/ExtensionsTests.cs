using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Mono.Cecil;
using Moq;
using Xunit;

namespace AutoFake.UnitTests
{
    public class ExtensionsTests
    {
        private TypeInfo GetTypeInfo() => new TypeInfo(new AssemblyReader(typeof(TestClass), new FakeOptions()), new FakeOptions(), new AssemblyPool());
        private MethodInfo GetMethodInfo(Expression<Action<TestClass>> func) => ((MethodCallExpression)func.Body).Method;

        [Fact]
        public void EquivalentTo_DifferentMethods_False()
        {
            var typeInfo = GetTypeInfo();

            var methodReference = typeInfo.GetMethods(m => m.Name == "Test" && m.Parameters.Count == 0).Single();
            var method = typeInfo.GetMethods(m => m.Name == "Other").Single();

            Assert.False(methodReference.EquivalentTo(method));
        }

        [Fact]
        public void EquivalentTo_DifferentArgumentsCount_False()
        {
            var typeInfo = GetTypeInfo();

            var methodReference = typeInfo.GetMethods(m => m.Name == "Test" && m.Parameters.Count == 0).Single();
            var method = typeInfo.GetMethods(m => m.Name == "Test" && m.Parameters.Count == 1 
				&& m.Parameters[0].ParameterType.FullName == "System.String").Single();

            Assert.False(methodReference.EquivalentTo(method));
        }

        [Fact]
        public void EquivalentTo_DifferentOverload_False()
        {
            var typeInfo = GetTypeInfo();

            var methodReference = GetStringOverloadMethodReference(typeInfo);
            var method = typeInfo.GetMethods(m => m.Name == "Test" && m.Parameters.Count == 1
				&& m.Parameters[0].ParameterType.FullName == "System.Int32").Single();

            Assert.False(methodReference.EquivalentTo(method));
        }

        private MethodDefinition GetStringOverloadMethodReference(TypeInfo typeInfo)
        {
            return typeInfo.GetMethods(m => m.Name == "Test" && m.Parameters.SingleOrDefault() != null &&
				m.Parameters.Single().ParameterType.FullName == typeof(string).FullName).Single();
        }

        [Fact]
        public void EquivalentTo_CorrectOverload_True()
        {
            var typeInfo = GetTypeInfo();

            var methodReference = GetStringOverloadMethodReference(typeInfo);
            var method = typeInfo.GetMethods(m => m.Name == "Test" && m.Parameters.Count == 1
				&& m.Parameters[0].ParameterType.FullName == "System.String").Single();


            Assert.True(methodReference.EquivalentTo(method));
        }

        [Theory, AutoMoqData]
        public void ToNonGeneric_GenericComparer_Success(IEqualityComparer<int> genericComparer)
        {
            var comparer = genericComparer.ToNonGeneric();

            Assert.Equal(genericComparer.Equals(5, 5), comparer.Equals(5, 5));
            Assert.Equal(genericComparer.GetHashCode(5), comparer.GetHashCode(5));
        }

        [Fact]
        public void ToTypeDefinition_TypeReference_Resolved()
        {
            var typeReference = new Mock<TypeReference>("", "");
            var def = typeReference.Object.ToTypeDefinition();

            typeReference.Verify(t => t.Resolve());
            Assert.NotSame(typeReference.Object, def);
        }

        [Theory, AutoMoqData]
        public void ToTypeDefinition_TypeDefinition_Cast(TypeDefinition typeDefinition)
        {
            var def = typeDefinition.ToTypeDefinition();

            Assert.Same(typeDefinition, def);
        }

        [Theory, AutoMoqData]
        public void ToFieldDefinition_FieldReference_Resolved(TypeReference type)
        {
            var fieldReference = new Mock<FieldReference>("", type);
            var def = fieldReference.Object.ToFieldDefinition();

            fieldReference.Verify(t => t.Resolve());
            Assert.NotSame(fieldReference.Object, def);
        }

        [Theory, AutoMoqData]
        public void ToFieldDefinition_FieldDefinition_Cast(FieldDefinition fieldDefinition)
        {
            var def = fieldDefinition.ToFieldDefinition();

            Assert.Same(fieldDefinition, def);
        }

        [Theory, AutoMoqData]
        public void ToMethodDefinition_TypeReference_Resolved(TypeReference type)
        {
            var methodReference = new Mock<MethodReference>("", type);
            var def = methodReference.Object.ToMethodDefinition();

            methodReference.Verify(t => t.Resolve());
            Assert.NotSame(methodReference.Object, def);
        }

        [Theory, AutoMoqData]
        public void ToMethodDefinition_TypeDefinition_Cast(MethodDefinition methodDefinition)
        {
            var def = methodDefinition.ToMethodDefinition();

            Assert.Same(methodDefinition, def);
        }

        private class TestClass
        {
            public void Test()
            {
            }

            public void Other()
            {
            }

            public void Test(string arg)
            {
            }

            public void Test(int arg)
            {
            }
        }
    }
}
