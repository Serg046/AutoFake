using AutoFake.Setup;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Moq;
using Xunit;

namespace AutoFake.UnitTests
{
    public class FakeMethodTests
    {
        private readonly Mock<IMock> _mock;
        private readonly MethodDefinition _method;
        private readonly Mock<IMethodMocker> _mocker;

        private readonly FakeMethod _fakeMethod;

        public FakeMethodTests()
        {
            _mock = new Mock<IMock>();
            _method = CreateMethod("testMethod");
            _mocker = new Mock<IMethodMocker>();
            _fakeMethod = new FakeMethod(_method, _mocker.Object);
        }

        [Fact]
        public void ApplyMock_IsAsyncMethod_RecursivelyCallsWithBody()
        {
            var expectedInstruction = Instruction.Create(OpCodes.Nop);
            var asyncMethod = CreateMethod("asyncTestMethod");
            asyncMethod.Body.Instructions.Add(expectedInstruction);
            _mock.Setup(m => m.IsAsyncMethod(_method, out asyncMethod)).Returns(true);

            _fakeMethod.ApplyMock(_mock.Object);

            _mock.Verify(m => m.IsInstalledInstruction(It.IsAny<ITypeInfo>(),
                It.Is<Instruction>(i => i == expectedInstruction)));
        }

        [Fact]
        public void ApplyMock_IsInstalledInstruction_CallsInjecting()
        {
            var expectedInstruction = Instruction.Create(OpCodes.Nop);
            _method.Body.Instructions.Add(expectedInstruction);
            _mock.Setup(m => m.IsInstalledInstruction(It.IsAny<ITypeInfo>(), expectedInstruction)).Returns(true);

            _fakeMethod.ApplyMock(_mock.Object);

            _mock.Verify(m => m.Inject(_mocker.Object, It.IsAny<ILProcessor>(), expectedInstruction));
        }

        [Theory]
        [InlineData(false, false, false)]
        [InlineData(true, false, false)]
        [InlineData(false, true, true)]
        [InlineData(true, true, false)]
        public void ApplyMock_IsFakeAssemblyMethod_RecursivelyCallsWithBody(bool isInstalled, bool isFakeAssemblyMethod, bool mustBeCalled)
        {
            var expectedInstruction = Instruction.Create(OpCodes.Nop);
            var methodToAnalyze = CreateMethod("methodToAnalyze");
            methodToAnalyze.Body.Instructions.Add(expectedInstruction);
            var installedInstruction = Instruction.Create(OpCodes.Call, methodToAnalyze);
            _method.Body.Instructions.Add(installedInstruction);
            if (isInstalled)
            {
                _mock.Setup(m => m.IsInstalledInstruction(It.IsAny<ITypeInfo>(), installedInstruction)).Returns(true);
            }
            if (isFakeAssemblyMethod)
            {
                var typeInfo = new Mock<ITypeInfo>();
                typeInfo.Setup(t => t.Module).Returns(ModuleDefinition.CreateModule("TestModule", ModuleKind.Dll));
                methodToAnalyze.DeclaringType.Scope = typeInfo.Object.Module;
                _mocker.Setup(m => m.TypeInfo).Returns(typeInfo.Object);
            }

            _fakeMethod.ApplyMock(_mock.Object);

            _mock.Verify(m => m.IsInstalledInstruction(It.IsAny<ITypeInfo>(),
                    It.Is<Instruction>(i => i == expectedInstruction)),
                mustBeCalled ? Times.AtLeastOnce() : Times.Never());
        }

        [Fact]
        public void ApplyMock_IsFakeAssemblyCtor_ConvertedToSourceAssembly()
        {
            var convertedCtor = CreateMethod(".ctor");
            var ctor = CreateMethod(".ctor");
            ctor.IsSpecialName = ctor.IsRuntimeSpecialName = true;
            var typeInfo = new Mock<ITypeInfo>();
            typeInfo.Setup(t => t.Module).Returns(ModuleDefinition.CreateModule("TestModule", ModuleKind.Dll));
            typeInfo.Setup(t => t.ConvertToSourceAssembly(ctor)).Returns(convertedCtor);
            ctor.DeclaringType.Scope = typeInfo.Object.Module;
            _mocker.Setup(m => m.TypeInfo).Returns(typeInfo.Object);
            var ctorInstruction = Instruction.Create(OpCodes.Call, ctor);
            _method.Body.Instructions.Add(ctorInstruction);

            _fakeMethod.ApplyMock(_mock.Object);

            Assert.NotEqual(ctor, ctorInstruction.Operand);
            Assert.Equal(convertedCtor, ctorInstruction.Operand);
        }

        private MethodDefinition CreateMethod(string name)
        {
            return new MethodDefinition(name, MethodAttributes.Public, new TypeReference("TestNamespace", "TestReturnType", null, null))
            {
                DeclaringType = new TypeDefinition("TestNamespace", "TestDeclaringType", TypeAttributes.Class)
            };
        }
    }
}
