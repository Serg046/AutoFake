using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AutoFake.Expression;
using Moq;
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
        public void Arguments_NoRuntimeValue_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => _visitor.Arguments);
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
        public void Visit_MethodWithCtorCallAsArgument_Success()
        {
            //Constructor call must be in lambda. Do not extract variable.
            var expression = GetMethodCallExpression(t => t.SomeMethod(new DateTime(2016, 8, 22)));

            _visitor.Visit(expression, expression.Method);

            Assert.Equal(1, _visitor.Arguments.Count);
            Assert.True(_visitor.Arguments[0].Check(new DateTime(2016, 8, 22)));
        }

        [Fact]
        public void Visit_MethodWithMethodCallAsArgument_Success()
        {
            var expression = GetMethodCallExpression(t => t.SomeMethod(TestClass.SomeStaticMethod(0)));

            _visitor.Visit(expression, expression.Method);

            Assert.Equal(1, _visitor.Arguments.Count);
            Assert.True(_visitor.Arguments[0].Check(0));
        }

        [Fact]
        public void Visit_MethodWithPropertyCallAsArgument_Success()
        {
            var expression = GetMethodCallExpression(t => t.SomeMethod(TestClass.SomeStaticProperty));

            _visitor.Visit(expression, expression.Method);

            Assert.Equal(1, _visitor.Arguments.Count);
            Assert.True(_visitor.Arguments[0].Check(2));
        }

        [Fact]
        public void Visit_MethodWithFieldCallAsArgument_Success()
        {
            var expression = GetMethodCallExpression(t => t.SomeMethod(TestClass.SomeStaticField));

            _visitor.Visit(expression, expression.Method);

            Assert.Equal(1, _visitor.Arguments.Count);
            Assert.True(_visitor.Arguments[0].Check(2));
        }

        [Fact]
        public void Visit_MethodWithCtorAndMethodCallsAsArgument_Success()
        {
            var expression = GetMethodCallExpression(t => t.SomeMethod(new DateTime(2016, 8, 22).AddDays(1)));

            _visitor.Visit(expression, expression.Method);

            Assert.Equal(1, _visitor.Arguments.Count);
            Assert.True(_visitor.Arguments[0].Check(new DateTime(2016, 8, 23)));
        }

        [Fact]
        public void Visit_MethodWithLambdaParameterAsArgument_Fails()
        {
            var expression = GetMethodCallExpression(t => t.SomeMethod(t));
            Assert.Throws<InvalidOperationException>(() => _visitor.Visit(expression, expression.Method));
        }

        [Fact]
        public void Visit_MethodWithSelfRef_Success()
        {
            var testClass = new MethodTestClass();
            var expression = GetMethodCallExpression(t => t.SomeMethod(testClass));

            _visitor.Visit(expression, expression.Method);

            Assert.Equal(1, _visitor.Arguments.Count);
            Assert.True(_visitor.Arguments[0].Check(testClass));
        }

        [Fact]
        public void Visit_MethodWithDifferentInstances_Success()
        {
            var expression = GetMethodCallExpression(t => t.SomeMethod(new MethodTestClass()));

            _visitor.Visit(expression, expression.Method);

            Assert.Equal(1, _visitor.Arguments.Count);
            Assert.False(_visitor.Arguments[0].Check(new MethodTestClass()));
        }

        [Fact]
        public void Visit_MethodWithLambdaArg_Success()
        {
            var date = new DateTime(2017, 1, 12);
            var expression = GetMethodCallExpression(t => t.SomeMethod(Arg.Is<DateTime>(a => a > date)));

            _visitor.Visit(expression, expression.Method);

            Assert.Equal(1, _visitor.Arguments.Count);
            Assert.True(_visitor.Arguments[0].Check(date.AddDays(1)));
        }

        [Fact]
        public void Visit_Property_ReturnsEmpty()
        {
            PropertyInfo property = null;

            _visitor.Visit(property);

            Assert.Empty(_visitor.Arguments);
        }

        [Fact]
        public void Visit_Field_ReturnsEmpty()
        {
            FieldInfo field = null;

            _visitor.Visit(field);

            Assert.Empty(_visitor.Arguments);
        }

        [Fact]
        public void Visit_CtorWithoutArgs_ReturnsEmpty()
        {
            var expression = GetNewExpression(() => new CtorTestClass());

            _visitor.Visit(expression, expression.Constructor);

            Assert.Equal(Enumerable.Empty<FakeArgument>(), _visitor.Arguments);
        }

        [Fact]
        public void Visit_CtorWithConstArg_Success()
        {
            var expression = GetNewExpression(() => new CtorTestClass(0));

            _visitor.Visit(expression, expression.Constructor);

            Assert.Equal(1, _visitor.Arguments.Count);
            Assert.True(_visitor.Arguments[0].Check(0));
        }

        [Fact]
        public void Visit_CtorWithMultipleArgs_Success()
        {
            var expression = GetNewExpression(() => new CtorTestClass(0, "0", "ab", GetType()));

            _visitor.Visit(expression, expression.Constructor);

            Assert.Equal(4, _visitor.Arguments.Count);
            Assert.True(_visitor.Arguments[0].Check(0));
            Assert.True(_visitor.Arguments[1].Check("0"));
            Assert.True(_visitor.Arguments[2].Check("ab"));
            Assert.True(_visitor.Arguments[3].Check(GetType()));
        }

        [Fact]
        public void Visit_CtorWithParamsArgs_Success()
        {
            var args1 = new[] { 0, 1 };
            var args2 = new object[] { 0, "1", GetType() };
            var expression = GetNewExpression(() => new CtorTestClass(-1, args1, args2));

            _visitor.Visit(expression, expression.Constructor);

            Assert.Equal(3, _visitor.Arguments.Count);
            Assert.True(_visitor.Arguments[0].Check(-1));
            Assert.True(_visitor.Arguments[1].Check(args1));
            Assert.True(_visitor.Arguments[2].Check(args2));
        }

        [Fact]
        public void Visit_CtorWithCtorCallAsArgument_Success()
        {
            //Constructor call must be in lambda. Do not extract variable.
            var expression = GetNewExpression(() => new CtorTestClass(new DateTime(2016, 8, 22)));

            _visitor.Visit(expression, expression.Constructor);

            Assert.Equal(1, _visitor.Arguments.Count);
            Assert.True(_visitor.Arguments[0].Check(new DateTime(2016, 8, 22)));
        }

        [Fact]
        public void Visit_CtorWithMethodCallAsArgument_Success()
        {
            var expression = GetNewExpression(() => new CtorTestClass(TestClass.SomeStaticMethod(0)));

            _visitor.Visit(expression, expression.Constructor);

            Assert.Equal(1, _visitor.Arguments.Count);
            Assert.True(_visitor.Arguments[0].Check(0));
        }

        [Fact]
        public void Visit_CtorWithPropertyCallAsArgument_Success()
        {
            var expression = GetNewExpression(() => new CtorTestClass(TestClass.SomeStaticProperty));

            _visitor.Visit(expression, expression.Constructor);

            Assert.Equal(1, _visitor.Arguments.Count);
            Assert.True(_visitor.Arguments[0].Check(2));
        }

        [Fact]
        public void Visit_CtorWithFieldCallAsArgument_Success()
        {
            var expression = GetNewExpression(() => new CtorTestClass(TestClass.SomeStaticField));

            _visitor.Visit(expression, expression.Constructor);

            Assert.Equal(1, _visitor.Arguments.Count);
            Assert.True(_visitor.Arguments[0].Check(2));
        }

        [Fact]
        public void Visit_CtorWithCtorAndMethodCallsAsArgument_Success()
        {
            var expression = GetNewExpression(() => new CtorTestClass(new DateTime(2016, 8, 22).AddDays(1)));

            _visitor.Visit(expression, expression.Constructor);

            Assert.Equal(1, _visitor.Arguments.Count);
            Assert.True(_visitor.Arguments[0].Check(new DateTime(2016, 8, 23)));
        }

        [Fact]
        public void Visit_CtorWithLambdaParameterAsArgument_Fails()
        {
            var expression = GetNewExpression(t => new CtorTestClass(t));
            Assert.Throws<InvalidOperationException>(() => _visitor.Visit(expression, expression.Constructor));
        }

        [Fact]
        public void Visit_CtorWithYourselfRef_Success()
        {
            var testClass = new CtorTestClass();
            var expression = GetNewExpression(() => new CtorTestClass(testClass));

            _visitor.Visit(expression, expression.Constructor);

            Assert.Equal(1, _visitor.Arguments.Count);
            Assert.True(_visitor.Arguments[0].Check(testClass));
        }

        [Fact]
        public void Visit_CtorWithDifferentInstances_Success()
        {
            var expression = GetNewExpression(() => new CtorTestClass(new CtorTestClass()));

            _visitor.Visit(expression, expression.Constructor);

            Assert.Equal(1, _visitor.Arguments.Count);
            Assert.False(_visitor.Arguments[0].Check(new TestClass()));
        }

        [Fact]
        public void Visit_CtorWithLambdaArg_Success()
        {
            var date = new DateTime(2017, 1, 12);
            var expression = GetNewExpression(() => new CtorTestClass(Arg.Is<DateTime>(a => a > date)));

            _visitor.Visit(expression, expression.Constructor);

            Assert.Equal(1, _visitor.Arguments.Count);
            Assert.True(_visitor.Arguments[0].Check(date.AddDays(1)));
        }

        [Fact]
        public void Visit_UnaryExpression_ReturnsEmpty()
        {
            var expression = GetMethodCallExpression(tst => tst.SomeMethod((object)5));

            _visitor.Visit(expression, expression.Method);

            Assert.Equal(1, _visitor.Arguments.Count);
            Assert.True(_visitor.Arguments.Single().Check(5));
        }

        [Fact]
        public void GetArgument_IsAny_SuccessfulArgumentChecker()
        {
            var expression = GetMethodCallExpression(tst => tst.SomeMethod(Arg.IsAny<int>()));

            _visitor.Visit(expression, expression.Method);

            Assert.IsType<SuccessfulArgumentChecker>(_visitor.Arguments.Single().Checker);
        }

        [Fact]
        public void GetArgument_IsWithEqualityComparer_EqualityArgumentChecker()
        {
            var expression = GetMethodCallExpression(tst => tst.SomeMethod(
                Arg.Is(5, Mock.Of<IEqualityComparer<int>>())));

            _visitor.Visit(expression, expression.Method);

            Assert.IsType<EqualityArgumentChecker>(_visitor.Arguments.Single().Checker);
        }

        private MethodCallExpression GetMethodCallExpression(Expression<Action<MethodTestClass>> expression)
            => (MethodCallExpression)expression.Body;

        private NewExpression GetNewExpression<T>(Expression<Func<T>> expression)
            => (NewExpression)expression.Body;

        private NewExpression GetNewExpression<T>(Expression<Func<CtorTestClass, T>> expression)
            => (NewExpression)expression.Body;

        private class TestClass
        {
            public static int SomeStaticProperty { get; } = 2;
            public static int SomeStaticField = 2;
            public static int SomeStaticMethod(int a) => a;
        }

        private class MethodTestClass
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

            public void SomeMethod(object a)
            {
            }

            public void SomeMethod(int a, string b, string c, Type d)
            {
            }

            public void SomeMethod(int a, int[] args1, params object[] args2)
            {
            }

            public void SomeMethod(MethodTestClass self)
            {
            }
        }

        private class CtorTestClass
        {
            public CtorTestClass()
            {
            }

            public CtorTestClass(int a)
            {
            }

            public CtorTestClass(DateTime a)
            {
            }

            public CtorTestClass(int a, string b, string c, Type d)
            {
            }

            public CtorTestClass(int a, int[] args1, params object[] args2)
            {
            }

            public CtorTestClass(CtorTestClass self)
            {
            }
        }
    }
}
