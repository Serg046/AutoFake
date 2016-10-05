using System;
using System.Linq;
using System.Reflection;
using AutoFake.Setup;
using GuardExtensions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Moq;
using Xunit;

namespace AutoFake.UnitTests
{
    public class FakeGeneratorTests
    {
        public class MethodInjectorFake : IMethodInjector
        {
            private MethodInjector _methodInjector;

            internal IMethodMocker MethodMocker
            {
                set
                {
                    _methodInjector = new MethodInjector(value);
                }
            }

            public virtual bool IsAsyncMethod(MethodDefinition method, out MethodDefinition asyncMethod)
                => _methodInjector.IsAsyncMethod(method, out asyncMethod);

            public virtual bool IsInstalledMethod(MethodReference method) => _methodInjector.IsInstalledMethod(method);

            public virtual bool IsMethodInstruction(Instruction instruction) => _methodInjector.IsMethodInstruction(instruction);

            public virtual void Process(ILProcessor ilProcessor, Instruction instruction)
                => _methodInjector.Process(ilProcessor, instruction);
        }

        private class TestClass
        {
            public void SimpleMethod()
            {
                var a = 5;
            }

            public void GetDateNow()
            {
                var a = DateTime.Now;
            }
        }

        private MethodInfo GetMethodInfo() => typeof(TestClass).GetMethods().First();
        private MethodInfo GetMethodInfo(string name) => typeof(TestClass).GetMethod(name);

        private FakeSetupPack GetFakeSetupPack() => GetFakeSetupPack(GetMethodInfo());

        private FakeSetupPack GetFakeSetupPack(MethodInfo method) => new FakeSetupPack()
        {
            Method = method,
            SetupArguments = new object[0]
        };

        private readonly Mock<IMocker> _mockerMock;
        private readonly Mock<MethodInjectorFake> _methodInjectorMock;
        private readonly FakeGenerator _fakeGenerator;

        public FakeGeneratorTests()
        {
            var typeInfo = new TypeInfo(typeof(TestClass), null);
            var mockedMemberInfo = new MockedMemberInfo(GetFakeSetupPack());

            _mockerMock = new Mock<IMocker>();
            _mockerMock.Setup(m => m.TypeInfo).Returns(typeInfo);
            _mockerMock.Setup(m => m.MemberInfo).Returns(mockedMemberInfo);

            _methodInjectorMock = new Mock<MethodInjectorFake>() { CallBase = true };
            _methodInjectorMock.Object.MethodMocker = _mockerMock.Object;

            var factoryMock = new Mock<MockerFactory>();
            factoryMock.Setup(f => f.CreateMocker(It.IsAny<TypeInfo>(), It.IsAny<FakeSetupPack>()))
                .Returns(_mockerMock.Object);
            factoryMock.Setup(f => f.CreateMethodInjector(It.IsAny<IMethodMocker>())).Returns(_methodInjectorMock.Object);

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
            var typeInfo = new TypeInfo(GetType(), null);
            var fakeGen = new FakeGenerator(typeInfo, new MockerFactory());
            Assert.Throws<ContractFailedException>(() => fakeGen.Save(null));
        }

        [Fact]
        public void Generate_InvalidInput_Throws()
        {
            var typeInfo = new TypeInfo(GetType(), null);
            var fakeGen = new FakeGenerator(typeInfo, new MockerFactory());

            var someMethodInfo = GetType().GetMethods()[0];
            if (someMethodInfo == null)
                throw new InvalidOperationException("MethodInfo is not found");

            var setups = new SetupCollection();
            setups.Add(new FakeSetupPack() {Method = someMethodInfo});

            Assert.Throws<ContractFailedException>(() => fakeGen.Generate(null, someMethodInfo));
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

        [Fact]
        public void Generate_NoInvocations_ProcessIsNotCalled()
        {
            var setup = GetFakeSetupPack(typeof(DateTime).GetProperty(nameof(DateTime.Now)).GetMethod);
            var setups = new SetupCollection { setup };
            var testMethod = GetMethodInfo(nameof(TestClass.SimpleMethod));

            _mockerMock.Setup(m => m.MemberInfo).Returns(new MockedMemberInfo(setup));
            _methodInjectorMock.Object.MethodMocker = _mockerMock.Object;

            _fakeGenerator.Generate(setups, testMethod);

            _methodInjectorMock.Verify(m => m.Process(It.IsAny<ILProcessor>(), It.IsAny<Instruction>()), Times.Never);
        }

        [Fact]
        public void Generate_MethodWithOneInvocation_ProcessOnce()
        {
            var setup = GetFakeSetupPack(typeof(DateTime).GetProperty(nameof(DateTime.Now)).GetMethod);
            var setups = new SetupCollection {setup};
            var testMethod = GetMethodInfo(nameof(TestClass.GetDateNow));

            _mockerMock.Setup(m => m.MemberInfo).Returns(new MockedMemberInfo(setup));
            _methodInjectorMock.Object.MethodMocker = _mockerMock.Object;

            _fakeGenerator.Generate(setups, testMethod);

            _methodInjectorMock.Verify(m => m.Process(It.IsAny<ILProcessor>(), It.IsAny<Instruction>()), Times.Once);
        }

        [Fact]
        public void Generate_ValidInput_AnalyzesOnlyClientCode()
        {
            var setup = GetFakeSetupPack(typeof(DateTime).GetProperty(nameof(DateTime.UtcNow)).GetMethod);
            var setups = new SetupCollection { setup };
            var testMethod = GetMethodInfo(nameof(TestClass.GetDateNow));

            _mockerMock.Setup(m => m.MemberInfo).Returns(new MockedMemberInfo(setup));
            _methodInjectorMock.Object.MethodMocker = _mockerMock.Object;

            _fakeGenerator.Generate(setups, testMethod);

            _methodInjectorMock.Verify(m => m.Process(It.IsAny<ILProcessor>(), It.IsAny<Instruction>()), Times.Never);
        }
    }
}
