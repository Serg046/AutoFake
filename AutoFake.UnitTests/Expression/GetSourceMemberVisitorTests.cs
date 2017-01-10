using System;
using System.Linq.Expressions;
using AutoFake.Expression;
using Xunit;

namespace AutoFake.UnitTests.Expression
{
    public class GetSourceMemberVisitorTests
    {
        private readonly GetSourceMemberVisitor _visitor;

        public GetSourceMemberVisitorTests()
        {
            _visitor = new GetSourceMemberVisitor();
        }

        [Fact]
        public void Visit_MethodExpression_Success()
        {
            Expression<Action<TestClass>> expression = t => t.Method();
            var methodCallExpression = expression.Body as MethodCallExpression;
            
            _visitor.Visit(methodCallExpression, methodCallExpression.Method);

            Assert.Equal(methodCallExpression.Method, _visitor.SourceMember);
        }

        [Fact]
        public void Visit_PropertyExpression_Success()
        {
            var property = typeof(TestClass).GetProperty(nameof(TestClass.Property));

            _visitor.Visit(property);

            Assert.Equal(property.GetMethod, _visitor.SourceMember);
        }

        [Fact]
        public void Visit_Field_Throws()
        {
            var field = typeof(TestClass).GetField(nameof(TestClass.Field));

            Assert.Throws<NotImplementedException>(() => _visitor.Visit(field));
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
