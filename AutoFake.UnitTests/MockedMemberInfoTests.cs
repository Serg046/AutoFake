using System;
using System.Collections.Generic;
using System.Reflection;
using AutoFake.Setup;
using Mono.Cecil;
using Xunit;

namespace AutoFake.UnitTests
{
    public class MockedMemberInfoTests
    {
        [Fact]
        public void Ctor_ArgumentFieldsInitialized()
        {
            Assert.Equal(0, new MockedMemberInfo(null, null, null).ArgumentsCount);
        }

        [Fact]
        public void GetArguments_NoArguments_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new MockedMemberInfo(null, null, null).GetArguments(0));
        }

        [Fact]
        public void GetArguments_SomeArguments_Returns()
        {
            var memberInfo = new MockedMemberInfo(null, null, null);

            var arguments = new FieldDefinition[2];
            memberInfo.AddArguments(arguments);

            Assert.Equal(arguments, memberInfo.GetArguments(0));
        }

        [Fact]
        public void AddArguments_SomeArguments_ArgumentsCountChanged()
        {
            var memberInfo = new MockedMemberInfo(null, null, null);

            memberInfo.AddArguments(new FieldDefinition[2]);
            Assert.Equal(1, memberInfo.ArgumentsCount);

            memberInfo.AddArguments(new FieldDefinition[2]);
            Assert.Equal(2, memberInfo.ArgumentsCount);
        }

        [Fact]
        public void EvaluateRetValueFieldName_Setup_ReturnsCorrectFieldName()
        {
            var sourceMember = new SourceMethod(GetType().GetMethod(nameof(Test)));
            var memberInfo = new MockedMemberInfo(new ReplaceableMock(sourceMember,
                new List<FakeArgument>(), null), null, "suffix");

            Assert.Equal("SystemInt32_Test_SystemObject_suffix", memberInfo.EvaluateRetValueFieldName());
        }

        public int Test(object arg) => 0;
    }
}
