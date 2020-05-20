using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoFake.Setup;
using AutoFake.Setup.Mocks;
using Moq;
using Xunit;

namespace AutoFake.UnitTests.Setup
{
    public class MockCollectionTests
    {
        [Theory, AutoMoqData]
        internal void Add_ValidInput_Added(
            Mock<MethodBase> method, ICollection<IMock> mocks,
            MockCollection sut)
        {
            sut.Add(method.Object, mocks);

            var item = sut.Single();
            Assert.Equal(method.Object, item.Method);
            Assert.Equal(mocks, item.Mocks);
        }
    }
}
