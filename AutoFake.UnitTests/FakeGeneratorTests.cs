using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoFake.Exceptions;
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
#pragma warning disable CS0219
            public void SimpleMethod()
            {
                var a = 5;
            }
#pragma warning restore CS0219

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
            SetupArguments = new List<FakeArgument>(),
            IsReturnObjectSet = true
        };

        private readonly Mock<IMocker> _mockerMock;
        private readonly Mock<MethodInjectorFake> _methodInjectorMock;
        private readonly FakeGenerator _fakeGenerator;

        public FakeGeneratorTests()
        {
            var typeInfo = new TypeInfo(typeof(TestClass), new List<FakeDependency>());
            var mockedMemberInfo = new MockedMemberInfo(GetFakeSetupPack(), GetType().GetMethods().First(), null);

            _mockerMock = new Mock<IMocker>();
            _mockerMock.Setup(m => m.TypeInfo).Returns(typeInfo);
            _mockerMock.Setup(m => m.MemberInfo).Returns(mockedMemberInfo);

            _methodInjectorMock = new Mock<MethodInjectorFake>() { CallBase = true };
            _methodInjectorMock.Object.MethodMocker = _mockerMock.Object;

            var factoryMock = new Mock<MockerFactory>();
            factoryMock.Setup(f => f.CreateMocker(It.IsAny<TypeInfo>(), It.IsAny<MockedMemberInfo>()))
                .Returns(_mockerMock.Object);
            factoryMock.Setup(f => f.CreateMethodInjector(It.IsAny<IMethodMocker>())).Returns(_methodInjectorMock.Object);

            _fakeGenerator = new FakeGenerator(typeInfo, factoryMock.Object, new GeneratedObject(typeInfo));
        }

        [Fact]
        public void Generate_IncorrectSetup_Throws()
        {
            var typeInfo = new TypeInfo(GetType(), new List<FakeDependency>());

            var someMethodInfo = GetType().GetMethods()[0];
            if (someMethodInfo == null)
                throw new InvalidOperationException("MethodInfo is not found");

            var setups = new SetupCollection();
            setups.Add(new FakeSetupPack() { Method = someMethodInfo, IsVoid = false, IsReturnObjectSet = false, IsVerification = false});

            var fakeGen = new FakeGenerator(typeInfo, new MockerFactory(), new GeneratedObject(typeInfo));

            Assert.Throws<SetupException>(() => fakeGen.Generate(setups, someMethodInfo));
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

            _mockerMock.Setup(m => m.MemberInfo).Returns(new MockedMemberInfo(setup, GetType().GetMethods().First(), null));
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

            _mockerMock.Setup(m => m.MemberInfo).Returns(new MockedMemberInfo(setup, GetType().GetMethods().First(), null));
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

            _mockerMock.Setup(m => m.MemberInfo).Returns(new MockedMemberInfo(setup, GetType().GetMethods().First(), null));
            _methodInjectorMock.Object.MethodMocker = _mockerMock.Object;

            _fakeGenerator.Generate(setups, testMethod);

            _methodInjectorMock.Verify(m => m.Process(It.IsAny<ILProcessor>(), It.IsAny<Instruction>()), Times.Never);
        }

        public static IEnumerable<object[]> GetCallbackFieldTestData()
        {
            yield return new object[] {null, false};
            yield return new object[] {new Action(() => Console.WriteLine(0)), true};
        }

        [Theory]
        [MemberData(nameof(GetCallbackFieldTestData))]
        public void Generate_ValidInput_CallbackFieldGenerated(Action installedAction, bool  mustBeCalled)
        {
            var setups = new SetupCollection();
            var setup = GetFakeSetupPack();
            setup.Callback = installedAction;
            setups.Add(setup);

            _fakeGenerator.Generate(setups, GetMethodInfo());

            if (mustBeCalled)
                _mockerMock.Verify(m => m.GenerateCallbackField());
            else
                _mockerMock.Verify(m => m.GenerateCallbackField(), Times.Never);
        }
    }
}
