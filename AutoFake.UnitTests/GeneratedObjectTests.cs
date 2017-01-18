using System.Collections.Generic;
using Xunit;

namespace AutoFake.UnitTests
{
    public class GeneratedObjectTests
    {
        [Fact]
        public void Ctor_MockedMembersInitialized()
        {
            Assert.NotNull(new GeneratedObject(null).MockedMembers);
        }
        
        [Fact]
        public void Build_TypeInfo_BuildsFake()
        {
            var generatedObject = new GeneratedObject(new TypeInfo(GetType(), new List<FakeDependency>()));

            generatedObject.Build();

            Assert.True(generatedObject.IsBuilt);
            Assert.NotNull(generatedObject.Type);
            Assert.NotNull(generatedObject.Instance);
        }
    }
}
