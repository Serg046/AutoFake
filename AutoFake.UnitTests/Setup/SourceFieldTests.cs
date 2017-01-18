using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoFake.Setup;
using Mono.Cecil.Cil;
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

        [Fact]
        public void IsCorrectInstruction_TheSameField_True()
        {
            var typeInfo = new TypeInfo(typeof(TestClass), new List<FakeDependency>());
            var sourceMember = new SourceField(typeof(TestClass).GetField(nameof(TestClass.Field)));
            var field = typeInfo.Fields.Single(m => m.Name == nameof(TestClass.Field));
            var instruction = Instruction.Create(OpCodes.Ldfld, field);

            Assert.True(sourceMember.IsCorrectInstruction(typeInfo, instruction));
        }

        [Fact]
        public void IsCorrectInstruction_IncorrectOpCode_False()
        {
            var typeInfo = new TypeInfo(typeof(TestClass), new List<FakeDependency>());
            var sourceMember = new SourceField(typeof(TestClass).GetField(nameof(TestClass.Field)));
            var field = typeInfo.Fields.Single(m => m.Name == nameof(TestClass.Field));
            var instruction = Instruction.Create(OpCodes.Stfld, field);

            Assert.False(sourceMember.IsCorrectInstruction(typeInfo, instruction));
        }

        [Fact]
        public void IsCorrectInstruction_DifferentTypes_False()
        {
            var typeInfo = new TypeInfo(typeof(TestClass), new List<FakeDependency>());
            var field = typeInfo.Fields.Single(m => m.Name == nameof(TestClass.Field));
            var instruction = Instruction.Create(OpCodes.Ldfld, field);
            var sourceMember = new SourceField(typeof(TestClass2).GetField(nameof(TestClass2.Field)));

            Assert.False(sourceMember.IsCorrectInstruction(typeInfo, instruction));
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
