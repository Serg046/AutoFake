using System;
using System.Linq;
using System.Linq.Expressions;
using AutoFake.Expression;
using Moq;
using Xunit;

namespace AutoFake.UnitTests.Expression
{
    public class TargetMemberVisitorTests
    {
        private readonly Mock<IMemberVisitor> _requestedVisitor; 
        private readonly TargetMemberVisitor _visitor;

        public TargetMemberVisitorTests()
        {
            _requestedVisitor = new Mock<IMemberVisitor>();
            _visitor = new TargetMemberVisitor(_requestedVisitor.Object, typeof(TargetClass));
        }

        [Fact]
        public void Visit_Method_Success()
        {
            var expression = (MethodCallExpression)((Expression<Action<TestClass>>)(t => t.Method(0))).Body;

            _visitor.Visit(expression, expression.Method);

            _requestedVisitor.Verify(v => v.Visit(expression, typeof(TargetClass).GetMethod(nameof(TargetClass.Method))));
        }

        [Fact]
        public void Visit_Property_Success()
        {
            var property = typeof(TestClass).GetProperty(nameof(TestClass.Property));

            _visitor.Visit(property);

            _requestedVisitor.Verify(v => v.Visit(typeof(TargetClass).GetProperty(nameof(TargetClass.Property))));
        }

        [Fact]
        public void Visit_Field_Success()
        {
            var field = typeof(TestClass).GetField(nameof(TestClass.Field));

            _visitor.Visit(field);

            _requestedVisitor.Verify(v => v.Visit(typeof(TargetClass).GetField(nameof(TargetClass.Field))));
        }

        [Fact]
        public void Visit_Constructor_Success()
        {
            var expression = (NewExpression)((Expression<Func<TestClass>>)(() => new TestClass())).Body;

            _visitor.Visit(expression, expression.Constructor);

            _requestedVisitor.Verify(v => v.Visit(expression, typeof(TargetClass).GetConstructors().Single()));
        }

        private class TestClass
        {
            public void Method(int arg)
            {
            }

            public int Property { get; }

            public int Field;
        }

        private class TargetClass
        {
            public void Method(int arg)
            {
            }

            public int Property { get; }

            public int Field;
        }
    }
}
