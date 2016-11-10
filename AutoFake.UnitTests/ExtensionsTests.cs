using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Mono.Cecil;
using Xunit;

namespace AutoFake.UnitTests
{
    public class ExtensionsTests
    {
        private TypeInfo GetTypeInfo() => new TypeInfo(typeof(TestClass), new List<FakeDependency>());
        private MethodInfo GetMethodInfo(Expression<Action<TestClass>> func) => ((MethodCallExpression)func.Body).Method;

        [Fact]
        public void EquivalentTo_DifferentMethods_False()
        {
            var typeInfo = GetTypeInfo();

            var methodReference = typeInfo.Methods.Single(m => m.Name == "Test" && m.Parameters.Count == 0);
            var method = GetMethodInfo(t => t.Other());

            Assert.False(methodReference.EquivalentTo(method));
        }

        [Fact]
        public void EquivalentTo_DifferentArgumentsCount_False()
        {
            var typeInfo = GetTypeInfo();

            var methodReference = typeInfo.Methods.Single(m => m.Name == "Test" && m.Parameters.Count == 0);
            var method = GetMethodInfo(t => t.Test(""));

            Assert.False(methodReference.EquivalentTo(method));
        }

        [Fact]
        public void EquivalentTo_DifferentOverload_False()
        {
            var typeInfo = GetTypeInfo();

            var methodReference = GetStringOverloadMethodReference(typeInfo);
            var method = GetMethodInfo(t => t.Test(1));

            Assert.False(methodReference.EquivalentTo(method));
        }

        private MethodDefinition GetStringOverloadMethodReference(TypeInfo typeInfo)
        {
            return typeInfo.Methods.Single(m => m.Name == "Test" && m.Parameters.SingleOrDefault() != null &&
                                                m.Parameters.Single().ParameterType.FullName == typeof(string).FullName);
        }

        [Fact]
        public void EquivalentTo_CorrectOverload_True()
        {
            var typeInfo = GetTypeInfo();

            var methodReference = GetStringOverloadMethodReference(typeInfo);
            var method = GetMethodInfo(t => t.Test(""));

            Assert.True(methodReference.EquivalentTo(method));
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
