using System;
using System.Linq;
using System.Linq.Expressions;
using AutoFake.Exceptions;
using AutoFake.Expression;
using Xunit;

namespace AutoFake.UnitTests.Expression
{
    public class GetTestMethodVisitorTests
    {
        private readonly GetTestMethodVisitor _visitor;

        public GetTestMethodVisitorTests()
        {
            _visitor = new GetTestMethodVisitor();
        }

        [Fact]
        public void Method_NoMethod_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => _visitor.Method);
        }

        [Fact]
        public void Visit_MethodExpression_Success()
        {
            Expression<Action<TestClass>> expression = t => t.Method();
            var methodCallExpression = expression.Body as MethodCallExpression;

            _visitor.Visit(methodCallExpression, methodCallExpression.Method);

            Assert.Equal(methodCallExpression.Method, _visitor.Method);
        }

        [Fact]
        public void Visit_PropertyExpression_Success()
        {
            var property = typeof(TestClass).GetProperty(nameof(TestClass.Property));

            _visitor.Visit(property);

            Assert.Equal(property.GetMethod, _visitor.Method);
        }

        [Fact]
        public void Visit_Field_Throws()
        {
            var field = typeof(TestClass).GetField(nameof(TestClass.Field));

            Assert.Throws<NotSupportedExpressionException>(() => _visitor.Visit(field));
        }

        [Fact]
        public void Visit_Constructor_Throws()
        {
            Expression<Func<TestClass>> expression = () => new TestClass();
            var newExpression = expression.Body as NewExpression;

            _visitor.Visit(newExpression, newExpression.Constructor);

            Assert.Equal(newExpression.Constructor, _visitor.Method);
        }

        private class TestClass
        {
            public static int Field;

            public int Property { get; }

            public void Method()
            {
            }
        }
    }
}
