using System;
using Moq;
using Xunit;

namespace AutoFake.UnitTests
{
    public class FakeArgumentTests
    {
        [Fact]
        public void Check_SimpleChecker_Success()
        {
            var checker = new Mock<IFakeArgumentChecker>();
            checker.Setup(c => c.Check(It.IsAny<object>())).Returns((int arg) => arg > 0);
            var argument = new FakeArgument(checker.Object);

            Assert.True(argument.Check(1));
            Assert.False(argument.Check(-1));
        }

        [Fact]
        public void Check_LambdaChacker_Success()
        {
            Func<int, bool> lambda = arg => arg > 0; 
            var checker = new LambdaChecker(lambda);
            var argument = new FakeArgument(checker);

            Assert.True(argument.Check(1));
            Assert.False(argument.Check(-1));
        }
    }
}
