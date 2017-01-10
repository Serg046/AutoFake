using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using AutoFake.Expression;
using Xunit;

namespace AutoFake.UnitTests.Expression
{
    public class GetValueMemberVisitorTests
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

        [Fact]
        public void RuntimeValue_ThrowsIfIsNotVisited()
        {
            var visitor = new GetValueMemberVisitor(new GeneratedObject(null)
            {
                Instance = new SomeInstanceType(),
                Type = typeof(SomeInstanceType)
            });

            Assert.Throws<InvalidOperationException>(() => visitor.RuntimeValue);

            visitor.Visit(typeof(SomeInstanceType).GetProperty(nameof(SomeInstanceType.SomeProperty)));

            Assert.Equal(2, visitor.RuntimeValue);
        }

        private static GeneratedObject GetGeneratedObject(object instance, Type type)
            => new GeneratedObject(null)
            {
                Instance = instance,
                Type = type
            };

        public static IEnumerable<object[]> GetVisitMethodTestData()
        {
            var generatedObject = GetGeneratedObject(new SomeInstanceTypeFake(), typeof(SomeInstanceTypeFake));
            Expression<Func<SomeInstanceType, int>> instanceExpr = s => s.SomeMethod(2);
            var method = generatedObject.Type.GetMethod(nameof(SomeInstanceTypeFake.SomeMethod));
            yield return new object[] {generatedObject, instanceExpr.Body, method, 3};

            Expression<Func<int>> staticExpr = () => SomeInstanceType.SomeStaticMethod(2);
            method = generatedObject.Type.GetMethod(nameof(SomeInstanceTypeFake.SomeStaticMethod));
            yield return new object[] {generatedObject, staticExpr.Body, method, 3};

            generatedObject = GetGeneratedObject(null, typeof(SomeStaticTypeFake));
            staticExpr = () => SomeStaticType.SomeStaticMethod(2);
            method = generatedObject.Type.GetMethod(nameof(SomeStaticTypeFake.SomeStaticMethod));
            yield return new object[] { generatedObject, staticExpr.Body, method, 3 };
        }

        [Theory]
        [MemberData(nameof(GetVisitMethodTestData))]
        internal void Visit_Method_Success(GeneratedObject obj, MethodCallExpression methodExpression, MethodInfo methodInfo, int expectedValue)
        {
            var visitor = new GetValueMemberVisitor(obj);

            visitor.Visit(methodExpression, methodInfo);
            
            Assert.Equal(expectedValue, visitor.RuntimeValue);
        }

        public static IEnumerable<object[]> GetVisitPropertyTestData()
        {
            var generatedObject = GetGeneratedObject(new SomeInstanceTypeFake(), typeof(SomeInstanceTypeFake));
            var property = generatedObject.Type.GetProperty(nameof(SomeInstanceTypeFake.SomeProperty));
            yield return new object[] {generatedObject, property, 3};

            property = generatedObject.Type.GetProperty(nameof(SomeInstanceTypeFake.SomeStaticProperty));
            yield return new object[] { generatedObject, property, 3 };

            generatedObject = GetGeneratedObject(null, typeof(SomeStaticTypeFake));
            property = generatedObject.Type.GetProperty(nameof(SomeStaticTypeFake.SomeStaticProperty));
            yield return new object[] { generatedObject, property, 3 };
        }

        [Theory]
        [MemberData(nameof(GetVisitPropertyTestData))]
        internal void Visit_Property_Success(GeneratedObject obj, PropertyInfo propertyInfo, int expectedValue)
        {
            var visitor = new GetValueMemberVisitor(obj);

            visitor.Visit(propertyInfo);

            Assert.Equal(expectedValue, visitor.RuntimeValue);
        }

        public static IEnumerable<object[]> GetVisitFieldTestData()
        {
            var generatedObject = GetGeneratedObject(new SomeInstanceTypeFake(), typeof(SomeInstanceTypeFake));
            var field = generatedObject.Type.GetField(nameof(SomeInstanceTypeFake.SomeField));
            yield return new object[] { generatedObject, field, 3 };

            field = generatedObject.Type.GetField(nameof(SomeInstanceTypeFake.SomeStaticField));
            yield return new object[] { generatedObject, field, 3 };

            generatedObject = GetGeneratedObject(null, typeof(SomeStaticTypeFake));
            field = generatedObject.Type.GetField(nameof(SomeStaticTypeFake.SomeStaticField));
            yield return new object[] { generatedObject, field, 3 };
        }

        [Theory]
        [MemberData(nameof(GetVisitFieldTestData))]
        internal void Visit_Field_Success(GeneratedObject obj, FieldInfo fieldInfo, int expectedValue)
        {
            var visitor = new GetValueMemberVisitor(obj);

            visitor.Visit(fieldInfo);

            Assert.Equal(expectedValue, visitor.RuntimeValue);
        }

        [Fact]
        public void Visit_MethodWithException_ThrowsTheSameException()
        {
            var generatedObject = GetGeneratedObject(new SomeInstanceTypeFake(), typeof(SomeInstanceTypeFake));
            Expression<Action<SomeInstanceTypeFake>> expr = s => s.FailMethod();
            var method = generatedObject.Type.GetMethod(nameof(SomeInstanceTypeFake.FailMethod));

            var visitor = new GetValueMemberVisitor(generatedObject);

            Assert.Throws<InvalidOperationException>(() => visitor.Visit((MethodCallExpression)expr.Body, method));
        }

        [Fact]
        public void Visit_PropertyWithException_ThrowsTheSameException()
        {
            var generatedObject = GetGeneratedObject(new SomeInstanceTypeFake(), typeof(SomeInstanceTypeFake));
            var property = generatedObject.Type.GetProperty(nameof(SomeInstanceTypeFake.FailProperty));

            var visitor = new GetValueMemberVisitor(generatedObject);

            Assert.Throws<InvalidOperationException>(() => visitor.Visit(property));
        }
    }
}
