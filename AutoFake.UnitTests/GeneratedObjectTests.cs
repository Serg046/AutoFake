using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using AutoFake.Exceptions;
using GuardExtensions;
using Moq;
using Xunit;

namespace AutoFake.UnitTests
{
    public class GeneratedObjectTests
    {
        private class SomeInstanceType
        {
            public int SomeMethod(int a) => a;
            public int SomeProperty { get; } = 2;
            public int SomeField = 2;
            public static int SomeStaticMethod(int a) => a;
            public static int SomeStaticProperty { get; } = 2;
            public static int SomeStaticField = 2;
        }

        private static class SomeStaticType
        {
            public static int SomeStaticMethod(int a) => a;
            public static int SomeStaticProperty { get; } = 2;
            public static int SomeStaticField = 2;
        }

        [Fact]
        public void Ctor_MockedMembersInitialized()
        {
            Assert.NotNull(new GeneratedObject().MockedMembers);
        }

        public static IEnumerable<object[]> AcceptMemberVisitorTestData()
        {
            var generatedObject = new GeneratedObject()
            {
                Instance = new SomeInstanceType(),
                Type = typeof(SomeInstanceType)
            };

            Expression<Action<IMemberVisitor>> verification = v => v.Visit(It.IsAny<MethodCallExpression>(), It.IsAny<MethodInfo>());
            Expression<Func<SomeInstanceType, object>> instanceExpr = e => e.SomeMethod(1);
            yield return new object[] { generatedObject, instanceExpr.Body, verification };

            verification = v => v.Visit(It.IsAny<MethodCallExpression>(), It.IsAny<MethodInfo>());
            Expression<Func<object>> staticExpr = () => SomeInstanceType.SomeStaticMethod(1);
            yield return new object[] { generatedObject, staticExpr.Body, verification };

            verification = v => v.Visit(It.IsAny<PropertyInfo>());
            instanceExpr = e => e.SomeProperty;
            yield return new object[] { generatedObject, instanceExpr.Body, verification };

            verification = v => v.Visit(It.IsAny<PropertyInfo>());
            staticExpr = () => SomeInstanceType.SomeStaticProperty;
            yield return new object[] { generatedObject, staticExpr.Body, verification };

            verification = v => v.Visit(It.IsAny<FieldInfo>());
            instanceExpr = e => e.SomeField;
            yield return new object[] { generatedObject, instanceExpr.Body, verification };

            verification = v => v.Visit(It.IsAny<FieldInfo>());
            staticExpr = () => SomeInstanceType.SomeStaticField;
            yield return new object[] { generatedObject, staticExpr.Body, verification };

            generatedObject = new GeneratedObject()
            {
                Instance = null,
                Type = typeof(SomeStaticType)
            };

            verification = v => v.Visit(It.IsAny<MethodCallExpression>(), It.IsAny<MethodInfo>());
            staticExpr = () => SomeStaticType.SomeStaticMethod(1);
            yield return new object[] { generatedObject, staticExpr.Body, verification };

            verification = v => v.Visit(It.IsAny<PropertyInfo>());
            staticExpr = () => SomeStaticType.SomeStaticProperty;
            yield return new object[] { generatedObject, staticExpr.Body, verification };

            verification = v => v.Visit(It.IsAny<FieldInfo>());
            staticExpr = () => SomeStaticType.SomeStaticField;
            yield return new object[] { generatedObject, staticExpr.Body, verification };
        }

        [Theory]
        [MemberData(nameof(AcceptMemberVisitorTestData))]
        internal void AcceptMemberVisitor_ValidData_Success(GeneratedObject generatedObject,
            Expression expression, Expression<Action<IMemberVisitor>> verification)
        {
            var visitorMock = new Mock<IMemberVisitor>();

            generatedObject.AcceptMemberVisitor(expression, visitorMock.Object);

            visitorMock.Verify(verification);
        }

        [Fact]
        public void AcceptMemberVisitor_InvalidExpression_Throws()
        {
            Assert.Throws<NotSupportedExpressionException>(
                () => new GeneratedObject().AcceptMemberVisitor(Expression.Constant(0), new GetValueMemberVisitor(new GeneratedObject())));
        }
    }
}
