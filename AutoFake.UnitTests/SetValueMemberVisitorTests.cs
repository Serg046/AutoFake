using System;
using AutoFake.Exceptions;
using Xunit;

namespace AutoFake.UnitTests
{
    public class SetValueMemberVisitorTests
    {
        private class SomeInstanceType
        {
            public int SomeProperty { get; set; } = 2;
            public int ReadOnlyProperty { get; } = 2;
            public int SomeField = 2;
            public static int SomeStaticProperty { get; set; } = 2;
            public static int SomeStaticField = 2;
        }

        private static class SomeStaticType
        {
            public static int SomeStaticProperty { get; set; } = 2;
            public static int SomeStaticField = 2;
        }

        [Fact]
        public void Visit_Method_Throws()
        {
            Assert.Throws<NotSupportedExpressionException>(() => new SetValueMemberVisitor(new GeneratedObject(null), null).Visit(null, null));
        }

        private static GeneratedObject GetGeneratedObject(object instance, Type type)
            => new GeneratedObject(null)
            {
                Instance = instance,
                Type = type
            };

        [Fact]
        public void Visit_Property_Success()
        {
            var obj = new SomeInstanceType();
            var visitor = new SetValueMemberVisitor(GetGeneratedObject(obj, typeof(SomeInstanceType)), 3);
            visitor.Visit(obj.GetType().GetProperty(nameof(SomeInstanceType.SomeProperty)));
            Assert.Equal(3, obj.SomeProperty);

            visitor.Visit(obj.GetType().GetProperty(nameof(SomeInstanceType.SomeStaticProperty)));
            Assert.Equal(3, SomeInstanceType.SomeStaticProperty);

            visitor.Visit(typeof(SomeStaticType).GetProperty(nameof(SomeStaticType.SomeStaticProperty)));
            Assert.Equal(3, SomeStaticType.SomeStaticProperty);
        }

        [Fact]
        public void Visit_Field_Success()
        {
            var obj = new SomeInstanceType();
            var visitor = new SetValueMemberVisitor(GetGeneratedObject(obj, typeof(SomeInstanceType)), 3);
            visitor.Visit(obj.GetType().GetField(nameof(SomeInstanceType.SomeField)));
            Assert.Equal(3, obj.SomeField);

            visitor.Visit(obj.GetType().GetField(nameof(SomeInstanceType.SomeStaticField)));
            Assert.Equal(3, SomeInstanceType.SomeStaticField);

            visitor.Visit(typeof(SomeStaticType).GetField(nameof(SomeStaticType.SomeStaticField)));
            Assert.Equal(3, SomeStaticType.SomeStaticField);
        }

        [Fact]
        public void Visit_InvalidProperty_Throws()
        {
            var obj = new SomeInstanceType();
            var visitor = new SetValueMemberVisitor(GetGeneratedObject(obj, typeof(SomeInstanceType)), 3);
            Assert.Throws<NotSupportedExpressionException>(
                () => visitor.Visit(obj.GetType().GetProperty(nameof(SomeInstanceType.ReadOnlyProperty))));
        }
    }
}
