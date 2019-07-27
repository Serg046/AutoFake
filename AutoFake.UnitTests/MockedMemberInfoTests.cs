using AutoFake.Expression;
using AutoFake.Setup;
using Moq;
using Xunit;

namespace AutoFake.UnitTests
{
    public class MockedMemberInfoTests
    {
        [Fact]
        public void GenerateFieldName_Setup_ReturnsCorrectFieldName()
        {
            var sourceMember = new SourceMethod(GetType().GetMethod(nameof(Test)));
            var expressionMock = new Mock<IInvocationExpression>();
            expressionMock.Setup(e => e.GetSourceMember()).Returns(sourceMember);
            var memberInfo = new MockedMemberInfo(new ReplaceMock(expressionMock.Object, null), "suffix");

            Assert.Equal("SystemInt32_Test_SystemObject_suffix", memberInfo.GenerateFieldName());
        }

        public int Test(object arg) => 0;
    }
}
