using System;
using System.Linq;
using System.Reflection;
using AutoFake.Setup;
using GuardExtensions;
using Moq;
using Xunit;

namespace AutoFake.UnitTests
{
    public class FakeGeneratorTests
    {
        private MethodInfo GetMethodInfo() => GetType().GetMethods().First();

        private FakeSetupPack GetFakeSetupPack() => new FakeSetupPack() {Method = GetMethodInfo()};

        private readonly FakeSetupPack _setup;
        private readonly MockedMemberInfo _mockedMemberInfo;
        private readonly Mock<IMocker> _mockerMock; 
        private readonly FakeGenerator _fakeGenerator;

        public FakeGeneratorTests()
        {
            var typeInfo = new TypeInfo(typeof(FakeGeneratorTests), null);

            _setup = GetFakeSetupPack();
            _mockedMemberInfo = new MockedMemberInfo(_setup);
            _mockerMock = new Mock<IMocker>();
            _mockerMock.Setup(m => m.TypeInfo).Returns(typeInfo);
            _mockerMock.Setup(m => m.MemberInfo).Returns(_mockedMemberInfo);

            var factoryMock = new Mock<MockerFactory>();
            factoryMock.Setup(f => f.CreateMocker(It.IsAny<TypeInfo>(), It.IsAny<FakeSetupPack>()))
                .Returns(_mockerMock.Object);
            _fakeGenerator = new FakeGenerator(typeInfo, factoryMock.Object);
        }

        [Fact]
        public void Ctor_Null_Throws()
        {
            Assert.Throws<ContractFailedException>(() => new FakeGenerator(null, new MockerFactory()));
            Assert.Throws<ContractFailedException>(() => new FakeGenerator(new TypeInfo(typeof(DateTime), null), null));
        }

        [Fact]
        public void Save_Null_Throws()
        {
            var typeInfo = new TypeInfo(null, null);
            var fakeGen = new FakeGenerator(typeInfo, new MockerFactory());
            Assert.Throws<ContractFailedException>(() => fakeGen.Save(null));
        }

        [Fact]
        public void Generate_InvalidInput_Throws()
        {
            var typeInfo = new TypeInfo(null, null);
            var fakeGen = new FakeGenerator(typeInfo, new MockerFactory());

            var someMethodInfo = GetType().GetMethods()[0];
            if (someMethodInfo == null)
                throw new InvalidOperationException("MethodInfo is not found");

            var setups = new SetupCollection();
            setups.Add(new FakeSetupPack() {Method = someMethodInfo});

            Assert.Throws<ContractFailedException>(() => fakeGen.Generate(null, someMethodInfo));
            Assert.Throws<ContractFailedException>(() => fakeGen.Generate(new SetupCollection(), someMethodInfo));
            Assert.Throws<ContractFailedException>(() => fakeGen.Generate(setups, null));
        }

        [Fact]
        public void Generate_ValidInput_ActualCallsGenerated()
        {
            var setups = new SetupCollection();
            setups.Add(GetFakeSetupPack());

            _fakeGenerator.Generate(setups, GetMethodInfo());

            _mockerMock.Verify(m => m.GenerateCallsCounter());
        }

        [Theory]
        [InlineData(false, true)]
        [InlineData(true, false)]
        public void Generate_IsNotVoid_RetValueFieldGenerated(bool isVoid, bool mustBeCalled)
        {
            var setups = new SetupCollection();
            var setup = GetFakeSetupPack();
            setup.IsVoid = isVoid;
            setups.Add(setup);

            _fakeGenerator.Generate(setups, GetMethodInfo());

            if (mustBeCalled)
                _mockerMock.Verify(m => m.GenerateRetValueField());
            else
                _mockerMock.Verify(m => m.GenerateRetValueField(), Times.Never);
        }
    }
}
