using AutoFake.Exceptions;
using GuardExtensions;
using Xunit;

namespace AutoFake.UnitTests
{
    public class FakeTests
    {
        [Fact]
        public void SaveFakeAssembly_Null_Throws()
        {
            Assert.Throws<ContractFailedException>(() => new Fake<FakeTests>().SaveFakeAssembly(null));
        }

        [Fact]
        public void Replace_Null_Throws()
        {
            Assert.Throws<ContractFailedException>(() => new Fake<FakeTests>().Replace(null));
        }

        [Fact]
        public void Verify_Null_Throws()
        {
            Assert.Throws<ContractFailedException>(() => new Fake<FakeTests>().Verify(null));
        }

        [Fact]
        public void Execute_Null_Throws()
        {
            Assert.Throws<ContractFailedException>(() => new Fake<FakeTests>().Execute(null));
        }

        [Fact]
        public void Reset_ClearsSetups()
        {
            var fake = new Fake<FakeTests>();

            fake.Verify((FakeTests f) => f.Reset_ClearsSetups())
                .ExpectedCallsCount(1);
            Assert.NotEmpty(fake.Mocks);

            fake.Reset();
            Assert.Empty(fake.Mocks);
        }

        [Fact]
        public void Rewrite_AfterExecuteInvocation_Throws()
        {
            var fake = new Fake<FakeTests>();
            fake.Execute(f => f.SomeMethod());

            Assert.Throws<FakeGeneretingException>(() => fake.Rewrite(f => f.SomeMethod()));
        }

        public void SomeMethod()
        {
        }
    }
}
