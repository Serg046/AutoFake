using System;
using AutoFake.Exceptions;
using AutoFake.Setup;
using Moq;
using Xunit;

namespace AutoFake.UnitTests.Setup
{
    public class SetupContextTests : IDisposable
    {
        private readonly Mock<IFakeArgumentChecker> _checkerMock;

        public SetupContextTests()
        {
            _checkerMock = new Mock<IFakeArgumentChecker>();
            _checkerMock.Setup(c => c.Check(It.IsAny<object>())).Returns((int arg) => arg == 7);
        }

        public void Dispose()
        {
            var setupContext = new SetupContext();
            if (setupContext.IsCheckerSet)
                setupContext.PopChecker();

            try
            {
                while (true)
                    setupContext.Dispose();
            }
            catch (MissedSetupContextException)
            {
            }
        }

        [Fact]
        public void SetCurrentChecker_Checker_CheckerIsSet()
        {
            var setupContext = new SetupContext();
            SetupContext.SetCurrentChecker(_checkerMock.Object);

            Assert.True(setupContext.IsCheckerSet);
            var checker = setupContext.PopChecker();
            Assert.False(checker.Check(-7));
            Assert.True(checker.Check(7));
        }

        [Fact]
        public void SetCurrentChecker_InvalidState_Throws()
        {
            using (new SetupContext())
            {
                SetupContext.SetCurrentChecker(_checkerMock.Object);
                Assert.Throws<SetupContextStateException>(() => SetupContext.SetCurrentChecker(_checkerMock.Object));
            }
        }

        [Fact]
        public void SetCurrentChecker_NoActiveInstance_Throws()
        {
            Assert.Throws<MissedSetupContextException>(() => SetupContext.SetCurrentChecker(_checkerMock.Object));
        }

        [Fact]
        public void PopChecker_ReturnsValueAndResetState()
        {
            var setupContext = new SetupContext();
            SetupContext.SetCurrentChecker(_checkerMock.Object);

            Assert.Equal(_checkerMock.Object, setupContext.PopChecker());
            Assert.False(setupContext.IsCheckerSet);
        }

        [Fact]
        public void PopChecker_NoActiveInstance_Throws()
        {
            var setupContext = new SetupContext();
            setupContext.Dispose();
            Assert.Throws<MissedSetupContextException>(() => setupContext.PopChecker());
        }

        [Fact]
        public void Dispose_ResetsState()
        {
            var setupContext = new SetupContext();
            SetupContext.SetCurrentChecker(_checkerMock.Object);
            setupContext.Dispose();

            Assert.False(setupContext.IsCheckerSet);
        }

        [Fact]
        public void Dispose_NoActiveInstance_Throws()
        {
            var setupContext = new SetupContext();
            setupContext.Dispose();
            Assert.Throws<MissedSetupContextException>(() => setupContext.Dispose());
        }
    }
}
