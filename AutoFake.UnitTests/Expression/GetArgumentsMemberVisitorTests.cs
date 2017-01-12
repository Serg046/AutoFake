using System;
using System.Linq;
using System.Linq.Expressions;
using AutoFake.Expression;
using Xunit;

namespace AutoFake.UnitTests.Expression
{
    public class GetArgumentsMemberVisitorTests
    {
        private readonly GetArgumentsMemberVisitor _visitor;

        public GetArgumentsMemberVisitorTests()
        {
            _visitor = new GetArgumentsMemberVisitor();
        }

        [Fact]
        public void Visit_MethodWithoutArgs_ReturnsEmpty()
        {
            var expression = GetMethodCallExpression(t => t.SomeMethod());

            _visitor.Visit(expression, expression.Method);

            Assert.Equal(Enumerable.Empty<FakeArgument>(), _visitor.Arguments);
        }

        [Fact]
        public void Visit_MethodWithConstArg_Success()
        {
            var expression = GetMethodCallExpression(t => t.SomeMethod(0));

            _visitor.Visit(expression, expression.Method);

            Assert.Equal(1, _visitor.Arguments.Count);
            Assert.True(_visitor.Arguments[0].Check(0));
        }

        [Fact]
        public void Visit_MethodWithMultipleArgs_Success()
        {
            var expression = GetMethodCallExpression(t => t.SomeMethod(0, "0", "ab", GetType()));

            _visitor.Visit(expression, expression.Method);

            Assert.Equal(4, _visitor.Arguments.Count);
            Assert.True(_visitor.Arguments[0].Check(0));
            Assert.True(_visitor.Arguments[1].Check("0"));
            Assert.True(_visitor.Arguments[2].Check("ab"));
            Assert.True(_visitor.Arguments[3].Check(GetType()));
        }

        [Fact]
        public void Visit_MethodWithParamsArgs_Success()
        {
            var args1 = new[] { 0, 1 };
            var args2 = new object[] { 0, "1", GetType() };
            var expression = GetMethodCallExpression(t => t.SomeMethod(-1, args1, args2));

            _visitor.Visit(expression, expression.Method);

            Assert.Equal(3, _visitor.Arguments.Count);
            Assert.True(_visitor.Arguments[0].Check(-1));
            Assert.True(_visitor.Arguments[1].Check(args1));
            Assert.True(_visitor.Arguments[2].Check(args2));
        }

        [Fact]
        public void Visit_CtorCallAsArgument_Success()
        {
            //Constructor call must be in lambda. Do not extract variable.
            var expression = GetMethodCallExpression(t => t.SomeMethod(new DateTime(2016, 8, 22)));

            _visitor.Visit(expression, expression.Method);

            Assert.Equal(1, _visitor.Arguments.Count);
            Assert.True(_visitor.Arguments[0].Check(new DateTime(2016, 8, 22)));
        }

        [Fact]
        public void Visit_MethodCallAsArgument_Success()
        {
            var expression = GetMethodCallExpression(t => t.SomeMethod(TestClass.SomeStaticMethod(0)));

            _visitor.Visit(expression, expression.Method);

            Assert.Equal(1, _visitor.Arguments.Count);
            Assert.True(_visitor.Arguments[0].Check(0));
        }

        [Fact]
        public void Visit_PropertyCallAsArgument_Success()
        {
            var expression = GetMethodCallExpression(t => t.SomeMethod(TestClass.SomeStaticProperty));

            _visitor.Visit(expression, expression.Method);

            Assert.Equal(1, _visitor.Arguments.Count);
            Assert.True(_visitor.Arguments[0].Check(2));
        }

        [Fact]
        public void Visit_FieldCallAsArgument_Success()
        {
            var expression = GetMethodCallExpression(t => t.SomeMethod(TestClass.SomeStaticField));

            _visitor.Visit(expression, expression.Method);

            Assert.Equal(1, _visitor.Arguments.Count);
            Assert.True(_visitor.Arguments[0].Check(2));
        }

        [Fact]
        public void Visit_CtorAndMethodCallsAsArgument_Success()
        {
            var expression = GetMethodCallExpression(t => t.SomeMethod(new DateTime(2016, 8, 22).AddDays(1)));

            _visitor.Visit(expression, expression.Method);

            Assert.Equal(1, _visitor.Arguments.Count);
            Assert.True(_visitor.Arguments[0].Check(new DateTime(2016, 8, 23)));
        }

        [Fact]
        public void Visit_LambdaParameterAsArgument_Fails()
        {
            var expression = GetMethodCallExpression(t => t.SomeMethod(t));
            Assert.Throws<InvalidOperationException>(() => _visitor.Visit(expression, expression.Method));
        }

        [Fact]
        public void Visit_YourselfRef_Success()
        {
            var testClass = new TestClass();
            var expression = GetMethodCallExpression(t => t.SomeMethod(testClass));

            _visitor.Visit(expression, expression.Method);

            Assert.Equal(1, _visitor.Arguments.Count);
            Assert.True(_visitor.Arguments[0].Check(testClass));
        }

        [Fact]
        public void Visit_DifferentInstances_Success()
        {
            var expression = GetMethodCallExpression(t => t.SomeMethod(new TestClass()));

            _visitor.Visit(expression, expression.Method);

            Assert.Equal(1, _visitor.Arguments.Count);
            Assert.False(_visitor.Arguments[0].Check(new TestClass()));
        }

        [Fact]
        public void Visit_LambdaArg_Success()
        {
            var date = new DateTime(2017, 1, 12);
            var expression = GetMethodCallExpression(t => t.SomeMethod(Arg.Is<DateTime>(a => a > date)));

            _visitor.Visit(expression, expression.Method);

            Assert.Equal(1, _visitor.Arguments.Count);
            Assert.True(_visitor.Arguments[0].Check(date.AddDays(1)));
        }

        private MethodCallExpression GetMethodCallExpression(Expression<Action<TestClass>> expression)
            => (MethodCallExpression)expression.Body;

        private class TestClass
        {
            public void SomeMethod()
            {
            }

            public void SomeMethod(int a)
            {
            }

            public void SomeMethod(DateTime a)
            {
            }

            public void SomeMethod(int a, string b, string c, Type d)
            {
            }

            public void SomeMethod(int a, int[] args1, params object[] args2)
            {
            }

            public void SomeMethod(TestClass self)
            {
            }

            public static int SomeStaticProperty { get; } = 2;
            public static int SomeStaticField = 2;
            public static int SomeStaticMethod(int a) => a;
        }
    }
}
