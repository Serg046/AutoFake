using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AutoFake.Exceptions;
using GuardExtensions;
using Xunit;

namespace AutoFake.UnitTests
{
    public class ExpressionUtilsTests
    {
        private MethodInfo GetMethod(string name)
            => GetMethod(name, new Type[0]);

        private MethodInfo GetMethod(string name, Type[] types)
            => GetMethod<ExpressionUtilsTests>(name, types);

        private MethodInfo GetMethod<T>(string name, Type[] types)
            => typeof(T).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic, null, types, null);

        public int SomeProperty { get; } = 1;
        public int SomeIntProperty { get; } = 1;
        
        public static int SomeStaticProperty { get; } = 1;

        public int SomeField = 1;
        public static int SomeStaticField = 1;

        private void SomeMethod()
        {
        }

        public object SomeMethod(object a) => a;
        public static object SomeStaticMethod(object a) => a;

        private void SomeMethod(int a, string b, string c, Type d)
        {
        }

        private void SomeMethod(int a, int[] args1, params object[] args2)
        {
        }

        private MethodCallExpression GetMethodCallExpression(Expression<Action> expression)
            => (MethodCallExpression)expression.Body;

        private MethodCallExpression GetMethodCallExpression<T>(Expression<Action<T>> expression)
            => (MethodCallExpression)expression.Body;

        private class SomeType
        {
            public void SomeMethod()
            {
            }

            public void SomeMethod(SomeType self)
            {
            }
        }

        private static class SomeStaticType
        {
            public static int SomeStaticMethod(int a) => a + 1;
            public static int SomeStaticProperty { get; } = 2;
            public static int SomeStaticField = 2;
        }

        [Fact]
        public void GetMethodInfo_Null_Throws()
        {
            Assert.Throws<ContractFailedException>(() => ExpressionUtils.GetMethodInfo(null));
        }

        [Fact]
        public void GetMethodInfo_UnsupportedExpression_Throws()
        {
            Expression<Func<int>> expression = () => 0;
            Assert.Throws<NotSupportedExpressionException>(() => ExpressionUtils.GetMethodInfo(expression));
        }

        public static IEnumerable<object[]> GetMethodInfoTestData()
        {
            var method = typeof(ExpressionUtilsTests).GetMethod(nameof(SomeMethod));
            Expression<Func<ExpressionUtilsTests, object>> instanceExpr = e => e.SomeMethod(1);
            yield return new object[] { instanceExpr, method };

            method = typeof(ExpressionUtilsTests).GetMethod(nameof(SomeStaticMethod));
            Expression<Func<object>> staticExpr = () => ExpressionUtilsTests.SomeStaticMethod(1);
            yield return new object[] { staticExpr, method };

            method = typeof(ExpressionUtilsTests).GetProperty(nameof(SomeProperty)).GetMethod;
            instanceExpr = e => e.SomeProperty;
            yield return new object[] { instanceExpr, method };

            method = typeof(ExpressionUtilsTests).GetProperty(nameof(SomeStaticProperty)).GetMethod;
            staticExpr = () => ExpressionUtilsTests.SomeStaticProperty;
            yield return new object[] { staticExpr, method };

            method = typeof(SomeStaticType).GetMethod(nameof(SomeStaticMethod));
            staticExpr = () => SomeStaticType.SomeStaticMethod(1);
            yield return new object[] { staticExpr, method };

            method = typeof(SomeStaticType).GetProperty(nameof(SomeStaticProperty)).GetMethod;
            staticExpr = () => SomeStaticType.SomeStaticProperty;
            yield return new object[] { staticExpr, method };
        }

        [Theory]
        [MemberData(nameof(GetMethodInfoTestData))]
        public void GetMethodInfo_ValidData_Success(LambdaExpression expression, MethodInfo expectedMethodInfo)
        {
            Assert.Equal(expectedMethodInfo, ExpressionUtils.GetMethodInfo(expression));
        }

        [Fact]
        public void GetArguments_Null_Throws()
        {
            Assert.Throws<ContractFailedException>(() => ExpressionUtils.GetArguments(null));
        }

        [Fact]
        public void GetArguments_MethodWithoutArgs_ReturnsEmpty()
        {
            var expression = GetMethodCallExpression(() => SomeMethod());

            Assert.Equal(Enumerable.Empty<object>(), ExpressionUtils.GetArguments(expression));
        }

        [Fact]
        public void GetArguments_MethodWithConstArg_Success()
        {
            var expression = GetMethodCallExpression(() => SomeMethod(0));
            var arguments = ExpressionUtils.GetArguments(expression).ToList();

            Assert.Equal(1, arguments.Count);
            Assert.Equal(0, arguments[0]);
        }

        [Fact]
        public void GetArguments_MethodWithMultipleArgs_Success()
        {
            var expression = GetMethodCallExpression(() => SomeMethod(0, "0", "ab", typeof(ExpressionUtilsTests)));
            var arguments = ExpressionUtils.GetArguments(expression).ToList();

            Assert.Equal(4, arguments.Count);
            Assert.Equal(0, arguments[0]);
            Assert.Equal("0", arguments[1]);
            Assert.Equal("ab", arguments[2]);
            Assert.Equal(typeof(ExpressionUtilsTests), arguments[3]);
        }

        [Fact]
        public void GetArguments_MethodWithParamsArgs_Success()
        {
            var args1 = new[] {0, 1};
            var args2 = new object[] {0, "1", typeof(ExpressionUtilsTests)};

            var expression = GetMethodCallExpression(() => SomeMethod(-1, args1, args2));
            var arguments = ExpressionUtils.GetArguments(expression).ToList();

            Assert.Equal(3, arguments.Count);
            Assert.Equal(-1, arguments[0]);
            Assert.Equal(args1, arguments[1]);
            Assert.Equal(args2, arguments[2]);
        }

        [Fact]
        public void GetArguments_CtorCallAsArgument_Success()
        {

            var expression = GetMethodCallExpression(() => SomeMethod(new DateTime(2016, 8, 22)));
            var arguments = ExpressionUtils.GetArguments(expression).ToList();

            Assert.Equal(1, arguments.Count);
            Assert.Equal(new DateTime(2016, 8, 22), arguments[0]);
        }

        [Fact]
        public void GetArguments_MethodCallAsArgument_Success()
        {
            var expression = GetMethodCallExpression(() => SomeMethod(SomeMethod(0)));
            var arguments = ExpressionUtils.GetArguments(expression).ToList();

            Assert.Equal(1, arguments.Count);
            Assert.Equal(0, arguments[0]);
        }

        [Fact]
        public void GetArguments_PropertyCallAsArgument_Success()
        {
            var expression = GetMethodCallExpression(() => SomeMethod(SomeProperty));
            var arguments = ExpressionUtils.GetArguments(expression).ToList();

            Assert.Equal(1, arguments.Count);
            Assert.Equal(SomeProperty, arguments[0]);
        }

        [Fact]
        public void GetArguments_CtorAndMethodCallsAsArgument_Success()
        {
            var expression = GetMethodCallExpression(() => SomeMethod(new DateTime(2016, 8, 22).AddDays(1)));
            var arguments = ExpressionUtils.GetArguments(expression).ToList();

            Assert.Equal(1, arguments.Count);
            Assert.Equal(new DateTime(2016, 8, 23), arguments[0]);
        }

        [Fact]
        public void GetArguments_ExternalMethod_Success()
        {
            var expression = GetMethodCallExpression<SomeType>(s => s.SomeMethod());
            var arguments = ExpressionUtils.GetArguments(expression).ToList();

            Assert.Equal(0, arguments.Count);
            Assert.Equal(Enumerable.Empty<object>(), arguments);
        }

        [Fact]
        public void GetArguments_ExternalType_UsingLambdaParameter_Fails()
        {
            var expression = GetMethodCallExpression<SomeType>(s => s.SomeMethod(s));
            Assert.Throws<NotSupportedExpressionException>(() => ExpressionUtils.GetArguments(expression).ToList());
        }

        [Fact]
        public void GetArguments_ExternalType_YourselfRef_Success()
        {
            var someType = new SomeType();
            var expression = GetMethodCallExpression<SomeType>(s => s.SomeMethod(someType));
            var arguments = ExpressionUtils.GetArguments(expression).ToList();

            Assert.Equal(1, arguments.Count);
            Assert.Equal(someType, arguments[0]);
        }

        [Fact]
        public void GetArguments_DifferentInstances_Success()
        {
            var expression = GetMethodCallExpression<SomeType>(s => s.SomeMethod(new SomeType()));
            var arguments = ExpressionUtils.GetArguments(expression).ToList();

            Assert.Equal(1, arguments.Count);
            Assert.NotEqual(new SomeType(), arguments[0]);
        }
    }
}
