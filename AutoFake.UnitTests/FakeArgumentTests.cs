using AutoFake.Setup;
using Moq;
using Xunit;

namespace AutoFake.UnitTests
{
    public class FakeArgumentTests
    {
        [Fact]
        public void Check_SimpleChecker_Checks()
        {
            var checker = new Mock<IFakeArgumentChecker>();
            checker.Setup(c => c.Check(It.IsAny<object>())).Returns((int arg) => arg > 0);
            var argument = new FakeArgument(checker.Object);

            Assert.True(argument.Check(1));
            Assert.False(argument.Check(-1));
        }

        [Fact]
        public void Check_DynamicChecker_Checks()
        {
            using (var setupContext = new SetupContext())
            {
                Arg.Is((int arg) => arg > 0);
                var checker = setupContext.PopChecker();
                var argument = new FakeArgument(checker);

                Assert.True(argument.Check(1));
                Assert.False(argument.Check(-1));
            }
        }
    }
}
