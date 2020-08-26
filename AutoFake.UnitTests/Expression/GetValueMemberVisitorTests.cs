﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using AutoFake.Exceptions;
using AutoFake.Expression;
using Xunit;

namespace AutoFake.UnitTests.Expression
{
    public class GetValueMemberVisitorTests
    {
        [Fact]
        public void RuntimeValue_ThrowsIfIsNotVisited()
        {
            var visitor = new GetValueMemberVisitor(new FakeObjectInfo(null, null, null));

            Assert.Throws<InvalidOperationException>(() => visitor.RuntimeValue);
        }

        public static IEnumerable<object[]> GetVisitMethodTestData()
        {
            var fakeObjectInfo = new FakeObjectInfo(null, null, typeof(SomeInstanceTypeFake), new SomeInstanceTypeFake());
            Expression<Func<SomeInstanceType, int>> instanceExpr = s => s.SomeMethod(2);
            var method = fakeObjectInfo.Type.GetMethod(nameof(SomeInstanceTypeFake.SomeMethod));
            yield return new object[] {fakeObjectInfo, instanceExpr.Body, method, 3};

            Expression<Func<int>> staticExpr = () => SomeInstanceType.SomeStaticMethod(2);
            method = fakeObjectInfo.Type.GetMethod(nameof(SomeInstanceTypeFake.SomeStaticMethod));
            yield return new object[] {fakeObjectInfo, staticExpr.Body, method, 3};

            fakeObjectInfo = new FakeObjectInfo(null, null, typeof(SomeStaticTypeFake));
            staticExpr = () => SomeStaticType.SomeStaticMethod(2);
            method = fakeObjectInfo.Type.GetMethod(nameof(SomeStaticTypeFake.SomeStaticMethod));
            yield return new object[] { fakeObjectInfo, staticExpr.Body, method, 3 };
        }

        [Theory]
        [MemberData(nameof(GetVisitMethodTestData))]
        internal void Visit_Method_Success(FakeObjectInfo obj, MethodCallExpression methodExpression, MethodInfo methodInfo, int expectedValue)
        {
            var visitor = new GetValueMemberVisitor(obj.Instance);

            visitor.Visit(methodExpression, methodInfo);
            
            Assert.Equal(expectedValue, visitor.RuntimeValue);
        }

        public static IEnumerable<object[]> GetVisitPropertyTestData()
        {
            var fakeObjectInfo = new FakeObjectInfo(null, null, typeof(SomeInstanceTypeFake), new SomeInstanceTypeFake());
            var property = fakeObjectInfo.Type.GetProperty(nameof(SomeInstanceTypeFake.SomeProperty));
            yield return new object[] {fakeObjectInfo, property, 3};

            property = fakeObjectInfo.Type.GetProperty(nameof(SomeInstanceTypeFake.SomeStaticProperty));
            yield return new object[] { fakeObjectInfo, property, 3 };

            fakeObjectInfo = new FakeObjectInfo(null, null, typeof(SomeStaticTypeFake));
            property = fakeObjectInfo.Type.GetProperty(nameof(SomeStaticTypeFake.SomeStaticProperty));
            yield return new object[] { fakeObjectInfo, property, 3 };
        }

        [Theory]
        [MemberData(nameof(GetVisitPropertyTestData))]
        internal void Visit_Property_Success(FakeObjectInfo obj, PropertyInfo propertyInfo, int expectedValue)
        {
            var visitor = new GetValueMemberVisitor(obj.Instance);

            visitor.Visit(propertyInfo);

            Assert.Equal(expectedValue, visitor.RuntimeValue);
        }

        public static IEnumerable<object[]> GetVisitFieldTestData()
        {
            var fakeObjectInfo = new FakeObjectInfo(null, null, typeof(SomeInstanceTypeFake), new SomeInstanceTypeFake());
            var field = fakeObjectInfo.Type.GetField(nameof(SomeInstanceTypeFake.SomeField));
            yield return new object[] { fakeObjectInfo, field, 3 };

            field = fakeObjectInfo.Type.GetField(nameof(SomeInstanceTypeFake.SomeStaticField));
            yield return new object[] { fakeObjectInfo, field, 3 };

            fakeObjectInfo = new FakeObjectInfo(null, null, typeof(SomeStaticTypeFake));
            field = fakeObjectInfo.Type.GetField(nameof(SomeStaticTypeFake.SomeStaticField));
            yield return new object[] { fakeObjectInfo, field, 3 };
        }

        [Theory]
        [MemberData(nameof(GetVisitFieldTestData))]
        internal void Visit_Field_Success(FakeObjectInfo obj, FieldInfo fieldInfo, int expectedValue)
        {
            var visitor = new GetValueMemberVisitor(obj.Instance);

            visitor.Visit(fieldInfo);

            Assert.Equal(expectedValue, visitor.RuntimeValue);
        }

        [Fact]
        public void Visit_MethodWithExceptionInside_ThrowsInnerException()
        {
            var fakeObjectInfo = new FakeObjectInfo(null, null, typeof(SomeInstanceTypeFake), new SomeInstanceTypeFake());
            Expression<Action<SomeInstanceTypeFake>> expr = s => s.FailMethod();
            var method = fakeObjectInfo.Type.GetMethod(nameof(SomeInstanceTypeFake.FailMethod));

            var visitor = new GetValueMemberVisitor(fakeObjectInfo.Instance);

            Assert.Throws<InvalidOperationException>(() => visitor.Visit((MethodCallExpression)expr.Body, method));
        }

        [Fact]
        public void Visit_PropertyWithExceptionInside_ThrowsInnerException()
        {
            var fakeObjectInfo = new FakeObjectInfo(null, null, typeof(SomeInstanceTypeFake), new SomeInstanceTypeFake());
            var property = fakeObjectInfo.Type.GetProperty(nameof(SomeInstanceTypeFake.FailProperty));

            var visitor = new GetValueMemberVisitor(fakeObjectInfo.Instance);

            Assert.Throws<InvalidOperationException>(() => visitor.Visit(property));
        }

        [Fact]
        public void Visit_PropertyWithException_ThrowsOriginalException()
        {
            var type = typeof(SomeInstanceTypeFake);
            var fakeObjectInfo = new FakeObjectInfo(null, null, type);
            var property = type.GetProperty(nameof(SomeInstanceTypeFake.SomeProperty));

            var visitor = new GetValueMemberVisitor(fakeObjectInfo);

            Assert.Throws<TargetException>(() => visitor.Visit(property));
        }

        [Fact]
        public void Visit_Ctor_Fails()
        {
            ConstructorInfo constructorInfo = null;
            var visitor = new GetValueMemberVisitor(new FakeObjectInfo(null, null, null));

            Assert.Throws<NotSupportedExpressionException>(() => visitor.Visit(null, constructorInfo));
        }

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

        private class SomeInstanceTypeFake
        {
            public int SomeMethod(int a) => a + 1;
            public int SomeProperty { get; } = 3;
            public int FailMethod() { throw new InvalidOperationException(); }

            public int FailProperty
            {
                get { throw new InvalidOperationException(); }
            }

            public int SomeField = 3;
            public static int SomeStaticMethod(int a) => a + 1;
            public static int SomeStaticProperty { get; } = 3;
            public static int SomeStaticField = 3;
        }

        private static class SomeStaticTypeFake
        {
            public static int SomeStaticMethod(int a) => a + 1;
            public static int SomeStaticProperty { get; } = 3;
            public static int SomeStaticField = 3;
        }
    }
}
