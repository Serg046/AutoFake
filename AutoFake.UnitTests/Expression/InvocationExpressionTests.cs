using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using AutoFake.Exceptions;
using AutoFake.Expression;
using Moq;
using Xunit;
using InvocationExpression = AutoFake.Expression.InvocationExpression;

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
        public void AcceptMemberVisitor_ValidData_Success(LambdaExpression expression, MemberInfo expectedMemberInfo)
        {
            var invocationExpression = new InvocationExpression(expression);

            invocationExpression.AcceptMemberVisitor(_memberVisitor.Object);

            switch (expectedMemberInfo)
            {
                case MethodInfo method:
                    _memberVisitor.Verify(v => v.Visit((MethodCallExpression)expression.Body, method));
                    break;
                case ConstructorInfo ctor:
                    _memberVisitor.Verify(v => v.Visit((NewExpression)expression.Body, ctor));
                    break;
                case PropertyInfo property:
                    _memberVisitor.Verify(v => v.Visit(property));
                    break;
                case FieldInfo field:
                    _memberVisitor.Verify(v => v.Visit(field));
                    break;
            }
        }

        [Fact]
        public void MatchArguments_TooManyArguments_Throws()
        {
            Expression<Action<TestClass>> methodExpr = e => e.Method();
            var expr = new InvocationExpression(methodExpr);
            var arguments = Enumerable.Range(0, byte.MaxValue + 1).Select(i => new object[] {i}).ToList();

            Assert.Throws<InvalidOperationException>(() => expr.MatchArguments(arguments, false, null));
        }

        [Fact]
        public void MatchArguments_ExpectedCallsMismatch_Throws()
        {
            Expression<Action<TestClass>> methodExpr = e => e.Method();
            var expr = new InvocationExpression(methodExpr);
            var arguments = Enumerable.Range(0, 2).Select(i => new object[] { i }).ToList();

            Assert.Throws<ExpectedCallsException>(() => expr.MatchArguments(arguments, false, count => count > 2));
        }

        [Fact]
        public void MatchArguments_ArgumentsMismatch_Throws()
        {
            Expression<Action<TestClass>> methodExpr = e => e.MethodWithArgs(5, "5");
            var expr = new InvocationExpression(methodExpr);
            var arguments = new[] { new object[] { 4, "4" }};

            Assert.Throws<VerifyException>(() => expr.MatchArguments(arguments, true, null));
        }

        [Theory]
        [InlineData(4, false)]
        [InlineData(5, true)]
        public void MatchArguments_ValidInput_Passes(int arg, bool checkArguments)
        {
            Expression<Action<TestClass>> methodExpr = e => e.MethodWithArgs(5, "5");
            var expr = new InvocationExpression(methodExpr);
            var arguments = new[] { new object[] { arg, arg.ToString() }};

            expr.MatchArguments(arguments, checkArguments, null);
        }

        [Theory]
        [InlineData(4, false)]
        [InlineData(5, true)]
        public void MatchArgumentsAsync_ValidInput_Passes(int arg, bool checkArguments)
        {
            Expression<Action<TestClass>> methodExpr = e => e.MethodWithArgs(5, "5");
            var expr = new InvocationExpression(methodExpr);
            var arguments = new[] { new object[] { arg, arg.ToString() } };

            expr.MatchArgumentsAsync(Task.CompletedTask, arguments, checkArguments, null);
        }

        [Theory]
        [InlineData(4, false)]
        [InlineData(5, true)]
        public void MatchArgumentsGenericAsync_ValidInput_Passes(int arg, bool checkArguments)
        {
            Expression<Action<TestClass>> methodExpr = e => e.MethodWithArgs(5, "5");
            var expr = new InvocationExpression(methodExpr);
            var arguments = new[] { new object[] { arg, arg.ToString() } };

            expr.MatchArgumentsGenericAsync(Task.FromResult(1), arguments, checkArguments, null);
        }

        [Fact]
        public void GetArguments_MethodExpr_Parameters()
        {
            var parameter = System.Linq.Expressions.Expression.Constant(5);
            var expression = System.Linq.Expressions.Expression.Call(
                System.Linq.Expressions.Expression.Constant(this),
                GetType().GetMethod(nameof(Equals)),
                System.Linq.Expressions.Expression.Convert(parameter, typeof(object))
            );
            var sut = new InvocationExpression(expression);

            var args = sut.GetArguments();

            Assert.True(args.Single().Check(5));
        }

        public static IEnumerable<object[]> GetAcceptMemberVisitorTestData()
        {
            var method = typeof(TestClass).GetMethod(nameof(TestClass.Method));
            Expression<Action<TestClass>> methodExpr = e => e.Method();
            yield return new object[] { methodExpr, method };

            method = typeof(TestClass).GetMethod(nameof(TestClass.StaticMethod));
            Expression<Action> staticMethodExpr = () => TestClass.StaticMethod();
            yield return new object[] { staticMethodExpr, method };

            method = typeof(TestClass).GetMethod(".ctor");
            Expression<Func<TestClass>> ctorExpr = () => new TestClass();
            yield return new object[] { ctorExpr, method };

            var property = typeof(TestClass).GetProperty(nameof(TestClass.Property));
            Expression<Func<TestClass, int>> propExpr = e => e.Property;
            yield return new object[] { propExpr, property };

            property = typeof(TestClass).GetProperty(nameof(TestClass.StaticProperty));
            Expression<Func<int>> staticPropExpr = () => TestClass.StaticProperty;
            yield return new object[] { staticPropExpr, property };

            var field = typeof(TestClass).GetField(nameof(TestClass.Field));
            Expression<Func<TestClass, int>> fldExpr = e => e.Field;
            yield return new object[] { fldExpr, field };

            field = typeof(TestClass).GetField(nameof(TestClass.StaticField));
            Expression<Func<int>> staticFldExpr = () => TestClass.StaticField;
            yield return new object[] { staticFldExpr, field };
            field = typeof(TestClass).GetField(nameof(TestClass.StaticField));

            Expression<Func<object>> staticFldExprWithCast = () => TestClass.StaticField;
            yield return new object[] { staticFldExprWithCast, field };
        }

#pragma warning disable 0649
        private class TestClass
        {
            public void Method() {}

            public void MethodWithArgs(int a, string b) { }

            public static void StaticMethod() {}

            public int Property { get; }

            public static int StaticProperty { get; }

            public int Field;

            public static int StaticField;
        }
    }
}
