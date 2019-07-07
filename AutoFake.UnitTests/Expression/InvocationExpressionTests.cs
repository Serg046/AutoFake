using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using AutoFake.Exceptions;
using AutoFake.Expression;
using Moq;
using Xunit;
using InvocationExpression = AutoFake.Expression.InvocationExpression;
using LinqExpression = System.Linq.Expressions.Expression;

namespace AutoFake.UnitTests.Expression
{
    public class InvocationExpressionTests
    {
        private readonly Mock<IMemberVisitor> _memberVisitor;

        public InvocationExpressionTests()
        {
            _memberVisitor = new Mock<IMemberVisitor>();
        }

        [Fact]
        public void AcceptMemberVisitor_UnsupportedExpression_Throws()
        {
            Expression<Func<int>> expression = () => 0;
            var invocationExpression = new InvocationExpression(expression);

            Assert.Throws<NotSupportedExpressionException>(() => invocationExpression.AcceptMemberVisitor(_memberVisitor.Object));
        }

        [Theory]
        [MemberData(nameof(GetAcceptMemberVisitorTestData))]
        public void AcceptMemberVisitor_ValidData_Success(LinqExpression expression, MethodCallExpression methodCallExpression, MemberInfo expectedMemberInfo)
        {
            var invocationExpression = new InvocationExpression(expression);

            invocationExpression.AcceptMemberVisitor(_memberVisitor.Object);

            if (methodCallExpression != null)
                _memberVisitor.Verify(v => v.Visit(methodCallExpression, (MethodInfo)expectedMemberInfo));

            var property = expectedMemberInfo as PropertyInfo;
            if (property != null)
                _memberVisitor.Verify(v => v.Visit(property));

            var field = expectedMemberInfo as FieldInfo;
            if (field != null)
                _memberVisitor.Verify(v => v.Visit(field));
        }

        public static IEnumerable<object[]> GetAcceptMemberVisitorTestData()
        {
            var method = typeof(TestClass).GetMethod(nameof(TestClass.Method));
            Expression<Action<TestClass>> methodExpr = e => e.Method();
            yield return new object[] { methodExpr, methodExpr.Body, method };

            method = typeof(TestClass).GetMethod(nameof(TestClass.StaticMethod));
            Expression<Action> staticMethodExpr = () => TestClass.StaticMethod();
            yield return new object[] { staticMethodExpr, staticMethodExpr.Body, method };

            var property = typeof(TestClass).GetProperty(nameof(TestClass.Property));
            Expression<Func<TestClass, int>> propExpr = e => e.Property;
            yield return new object[] { propExpr, null, property };

            property = typeof(TestClass).GetProperty(nameof(TestClass.StaticProperty));
            Expression<Func<int>> staticPropExpr = () => TestClass.StaticProperty;
            yield return new object[] { staticPropExpr, null, property };


            var field = typeof(TestClass).GetField(nameof(TestClass.Field));
            Expression<Func<TestClass, int>> fldExpr = e => e.Field;
            yield return new object[] { fldExpr, null, field };

            field = typeof(TestClass).GetField(nameof(TestClass.StaticField));
            Expression<Func<int>> staticFldExpr = () => TestClass.StaticField;
            yield return new object[] { staticFldExpr, null, field };
            field = typeof(TestClass).GetField(nameof(TestClass.StaticField));

            Expression<Func<object>> staticFldExprWithCast = () => TestClass.StaticField;
            yield return new object[] { staticFldExprWithCast, null, field };
        }

        private event EventHandler _testEvent;

        private class TestClass
        {
            public void Method()
            {
            }

            public static void StaticMethod()
            {
            }

            public int Property { get; }

            public static int StaticProperty { get; }

            public int Field;

            public static int StaticField;
        }
    }
}
