using System;
using System.Collections.Generic;
using System.Linq;
using AutoFake.Setup;
using Moq;
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
        
        [Theory]
        [InlineData(typeof(GeneratedObjectTests))]
        [InlineData(typeof(Enumerable))]
        public void Build_TypeInfo_BuildsFake(Type type)
        {
            var generatedObject = new GeneratedObject(new TypeInfo(type, new List<FakeDependency>()));

            generatedObject.Build();

            Assert.True(generatedObject.IsBuilt);
            Assert.NotNull(generatedObject.Type);
            var instanceAssert = type.IsAbstract && type.IsSealed ? (Action<object>)Assert.Null : Assert.NotNull;
            instanceAssert(generatedObject.Instance);
        }

        [Fact]
        public void Build_MockedMembers_Initialize()
        {
            var generatedObject = new GeneratedObject(new TypeInfo(GetType(), new List<FakeDependency>()));
            var mock = new Mock<IMock>();
            generatedObject.MockedMembers.Add(new MockedMemberInfo(mock.Object, null));

            generatedObject.Build();

            mock.Verify(m => m.Initialize(generatedObject.MockedMembers.Single(), generatedObject));
        }
    }
}
