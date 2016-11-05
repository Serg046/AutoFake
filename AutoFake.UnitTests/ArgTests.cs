using System;
using AutoFake.Setup;
using Xunit;

namespace AutoFake.UnitTests
{
    public class ArgTests
    {
        [Fact]
        public void IsNull_ReferenceType_Success()
        {
            var dependency = Arg.IsNull<string>();

            Assert.Null(dependency.Instance);
            Assert.Equal(typeof(string), dependency.Type);
        }

        [Fact]
        public void IsNull_NullableType_Success()
        {
            var dependency = Arg.IsNull<int?>();

            Assert.Null(dependency.Instance);
            Assert.Equal(typeof(int?), dependency.Type);
        }

        [Fact]
        public void IsNull_ValueType_Throws()
        {
            Assert.Throws<NotSupportedException>(() => Arg.IsNull<int>());
        }

        [Fact]
        public void Is_Func_CheckerIsSet()
        {
            using (var setupContext = new SetupContext())
            {
                Arg.Is((int arg) => arg == 7);

                Assert.True(setupContext.IsCheckerSet);
                var checker = setupContext.PopChecker();
                Assert.False(checker.Check(-7));
                Assert.True(checker.Check(7));
            }
        }
    }
}
