using System;
using GuardExtensions;
using Xunit;

namespace AutoFake.UnitTests
{
    public class MockerFactoryTests
    {
        [Fact]
        public void CreateMocker_Null_Throws()
        {
            Assert.Throws<ContractFailedException>(
                () => new MockerFactory().CreateMocker(null, new MockedMemberInfo(null, null, null)));
            Assert.Throws<ContractFailedException>(
                () => new MockerFactory().CreateMocker(new TypeInfo(typeof(DateTime), null), null));
        }

        [Fact]
        public void CreateMethodInjector_Null_Throws()
        {
            Assert.Throws<ContractFailedException>(() => new MockerFactory().CreateMethodInjector(null));
        }
    }
}
