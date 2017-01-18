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

            Assert.Equal(methodCallExpression.Method.Name, _visitor.SourceMember.Name);
            Assert.Equal(methodCallExpression.Method.ReturnType, _visitor.SourceMember.ReturnType);
            Assert.Equal(methodCallExpression.Method.GetParameters(), _visitor.SourceMember.GetParameters());
        }

        [Fact]
        public void Visit_PropertyExpression_Success()
        {
            var property = typeof(TestClass).GetProperty(nameof(TestClass.Property));

            _visitor.Visit(property);

            Assert.Equal(property.GetMethod.Name, _visitor.SourceMember.Name);
            Assert.Equal(property.GetMethod.ReturnType, _visitor.SourceMember.ReturnType);
        }

        [Fact]
        public void Visit_Field_Throws()
        {
            var field = typeof(TestClass).GetField(nameof(TestClass.Field));

            _visitor.Visit(field);

            Assert.Equal(field.Name, _visitor.SourceMember.Name);
            Assert.Equal(field.FieldType, _visitor.SourceMember.ReturnType);
        }

        [Fact]
        public void Visit_NewExpression_Success()
        {
            Expression<Func<TestClass>> expression = () => new TestClass();
            var newExpression = expression.Body as NewExpression;

            _visitor.Visit(newExpression, newExpression.Constructor);

            Assert.Equal(newExpression.Constructor.Name, _visitor.SourceMember.Name);
            Assert.Equal(newExpression.Constructor.DeclaringType, _visitor.SourceMember.ReturnType);
            Assert.Equal(newExpression.Constructor.GetParameters(), _visitor.SourceMember.GetParameters());
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
