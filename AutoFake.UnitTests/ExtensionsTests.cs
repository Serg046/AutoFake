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
            var method = typeInfo.Methods.Single(m => m.Name == "Other");

            Assert.False(methodReference.EquivalentTo(method));
        }

        [Fact]
        public void EquivalentTo_DifferentArgumentsCount_False()
        {
            var typeInfo = GetTypeInfo();

            var methodReference = typeInfo.Methods.Single(m => m.Name == "Test" && m.Parameters.Count == 0);
            var method = typeInfo.Methods.Single(m => m.Name == "Test" && m.Parameters.Count == 1
                                                                       && m.Parameters[0].ParameterType.FullName == "System.String");

            Assert.False(methodReference.EquivalentTo(method));
        }

        [Fact]
        public void EquivalentTo_DifferentOverload_False()
        {
            var typeInfo = GetTypeInfo();

            var methodReference = GetStringOverloadMethodReference(typeInfo);
            var method = typeInfo.Methods.Single(m => m.Name == "Test" && m.Parameters.Count == 1
                                                                       && m.Parameters[0].ParameterType.FullName == "System.Int32");

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
            var method = typeInfo.Methods.Single(m => m.Name == "Test" && m.Parameters.Count == 1
                                                                       && m.Parameters[0].ParameterType.FullName == "System.String");


            Assert.True(methodReference.EquivalentTo(method));
        }

        [Fact]
        public void ToMethodDescriptor_Action_Success()
        {
            Action action = () => { };

            var descriptor = action.ToMethodDescriptor();

            Assert.Equal(action.Method.DeclaringType.FullName, descriptor.DeclaringType);
            Assert.Equal(action.Method.Name, descriptor.Name);
        }

        [Fact]
        public void ToMethodDescriptor_CompiledAction_Success()
        {
            Expression<Func<int>> expression = () => 5;
            var func = expression.Compile();

            var descriptor = func.ToMethodDescriptor();

            Assert.Null(func.Method.DeclaringType);
            Assert.Null(descriptor.DeclaringType);
            Assert.Equal(func.Method.Name, descriptor.Name);
        }

        [Fact]
        public void ToClosureDescriptor_Closure_Success()
        {
            var date = DateTime.Now;
            Action action = () => Console.WriteLine(date - date);
            var typeInfo = new TypeInfo(action.Method.DeclaringType, new FakeDependency[0]);

            var descriptor = action.ToClosureDescriptor(typeInfo.Module);

            Assert.Equal(action.Method.DeclaringType.FullName, descriptor.DeclaringType);
            Assert.Equal(action.Method.Name, descriptor.Name);
            Assert.Single(descriptor.CapturedMembers, d => d.Field.Name == nameof(date)
                                                           && d.Instance.Equals(date));
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
