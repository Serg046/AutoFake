using Xunit;

namespace AutoFake.UnitTests
{
    public class MockerFactoryTests
    {
        [Fact]
        public void CreateMocker_Input_ReturnsValidMocker()
        {
            var factory = new MockerFactory();
            var typeInfo = new TypeInfo(GetType(), null);
            var memberInfo = new MockedMemberInfo(null, null);

            var mocker = factory.CreateMocker(typeInfo, memberInfo);

            Assert.Equal(typeInfo, mocker.TypeInfo);
            Assert.Equal(memberInfo, mocker.MemberInfo);
        }
    }
}
