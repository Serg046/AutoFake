using System.Collections.Generic;
using System.Reflection;
using AutoFake.Setup;
using Mono.Cecil.Cil;
using Moq;
using Xunit;

namespace AutoFake.UnitTests.Setup
{
    public class SourceFieldTests
    {
        [Fact]
        public void Name_FieldInfo_ReturnsName()
        {
            var field = typeof(TestClass2).GetField(nameof(TestClass.Field));
            var sourceField = new SourceField(field);

            Assert.Equal(field.Name, sourceField.Name);
        }

        [Fact]
        public void ReturnType_FieldInfo_ReturnsType()
        {
            var field = typeof(TestClass2).GetField(nameof(TestClass.Field));
            var sourceField = new SourceField(field);

            Assert.Equal(field.FieldType, sourceField.ReturnType);
        }

        [Theory]
        [MemberData(nameof(FieldAccessInstructions))]
        public void IsSourceInstruction_TheSameField_True(OpCode fldInstruction)
        {
            var typeInfo = new TypeInfo(typeof(TestClass), new List<FakeDependency>(), new FakeOptions());
            var sourceMember = new SourceField(typeof(TestClass).GetField(nameof(TestClass.Field)));
            var field = typeInfo.GetField(m => m.Name == nameof(TestClass.Field));
            var instruction = Instruction.Create(fldInstruction, field);

            Assert.True(sourceMember.IsSourceInstruction(typeInfo, instruction));
        }

        public static IEnumerable<object[]> FieldAccessInstructions =>
            new List<object[]>
            {
                new object [] {OpCodes.Ldfld},
                new object [] {OpCodes.Ldsfld},
                new object [] {OpCodes.Ldflda},
                new object [] {OpCodes.Ldsflda},
            };

        [Fact]
        public void IsSourceInstruction_IncorrectOpCode_False()
        {
            var typeInfo = new TypeInfo(typeof(TestClass), new List<FakeDependency>(), new FakeOptions());
            var sourceMember = new SourceField(typeof(TestClass).GetField(nameof(TestClass.Field)));
            var field = typeInfo.GetField(m => m.Name == nameof(TestClass.Field));
            var instruction = Instruction.Create(OpCodes.Stfld, field);

            Assert.False(sourceMember.IsSourceInstruction(typeInfo, instruction));
        }

        [Fact]
        public void IsSourceInstruction_DifferentTypes_False()
        {
            var typeInfo = new TypeInfo(typeof(TestClass), new List<FakeDependency>(), new FakeOptions());
            var field = typeInfo.GetField(m => m.Name == nameof(TestClass.Field));
            var instruction = Instruction.Create(OpCodes.Ldfld, field);
            var sourceMember = new SourceField(typeof(TestClass2).GetField(nameof(TestClass2.Field)));

            Assert.False(sourceMember.IsSourceInstruction(typeInfo, instruction));
        }

        [Fact]
        public void GetParameters_FieldInfo_ReturnsName()
        {
            var field = typeof(TestClass2).GetField(nameof(TestClass.Field));
            var sourceField = new SourceField(field);

            Assert.Equal(new ParameterInfo[0], sourceField.GetParameters());
        }

        [Theory]
        [InlineData(nameof(TestClass.Field), true)]
        [InlineData(nameof(TestClass.StaticField), false)]
        public void HasStackInstance_Field_Success(string fieldName, bool expectedResult)
        {
            var field = typeof(TestClass).GetField(fieldName);
            var sourceField = new SourceField(field);

            Assert.Equal(expectedResult, sourceField.HasStackInstance);
        }

        [Fact]
        public void Equals_TheSameField_True()
        {
            var field = typeof(TestClass2).GetField(nameof(TestClass.Field));
            var sourceField1 = new SourceField(field);
            var sourceField2 = new SourceField(field);

            Assert.True(sourceField1.Equals(sourceField2));
        }

        [Fact]
        public void GetHashCode_Field_TheSameHashCodes()
        {
            var field = typeof(TestClass2).GetField(nameof(TestClass.Field));
            var sourceField = new SourceField(field);

            Assert.Equal(field.GetHashCode(), sourceField.GetHashCode());
        }

        [Fact]
        public void ToString_Field_TheSameStrings()
        {
            var field = typeof(TestClass2).GetField(nameof(TestClass.Field));
            var sourceField = new SourceField(field);

            Assert.Equal(field.ToString(), sourceField.ToString());
        }

        [Fact]
        public void Equals_ValidInput_True()
        {
            var field = new Mock<FieldInfo>();
            var field2 = new Mock<FieldInfo>();
            field.Setup(m => m.Equals(It.IsAny<FieldInfo>())).Returns(true);
            field.Setup(m => m.Equals(field2.Object)).Returns(false);
            field2.Setup(m => m.Equals(field.Object)).Returns(false);

            var sut = new SourceField(field.Object);

            Assert.NotEqual(field.Object, field2.Object);
            Assert.False(sut.Equals(null));
            Assert.False(sut.Equals(new object()));
            Assert.False(sut.Equals(new SourceField(field2.Object)));
            Assert.True(sut.Equals(new SourceField(field.Object)));
        }

#pragma warning disable 0649
        private class TestClass
        {
            public int Field;
            public static int StaticField;
        }

        private class TestClass2
        {
            public int Field;
        }
    }
}
