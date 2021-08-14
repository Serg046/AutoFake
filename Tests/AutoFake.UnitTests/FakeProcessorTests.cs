using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AutoFake.Setup.Mocks;
using AutoFixture.Xunit2;
using FluentAssertions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Moq;
using Xunit;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using OpCodes = Mono.Cecil.Cil.OpCodes;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace AutoFake.UnitTests
{
    public class FakeProcessorTests
    {
        private readonly FakeProcessor _fakeProcessor;
        private readonly TypeInfo _typeInfo;

        public FakeProcessorTests()
        {
            _typeInfo = new TypeInfo(typeof(TestClass), new List<FakeDependency>(), new FakeOptions());
            _fakeProcessor = new FakeProcessor(_typeInfo, new FakeOptions());
        }

        [Fact]
        public void ProcessSourceMethod_Mock_PreparedForInjecting()
        {
            var testMethod = GetMethodInfo(nameof(TestClass.SimpleMethod));
            var mock = new Mock<IMock>();

            _fakeProcessor.ProcessMethod(new[] { mock.Object }, testMethod);

            mock.Verify(m => m.BeforeInjection(It.Is<MethodDefinition>(met => met.Name == testMethod.Name)));
        }

        [Fact]
        public void ProcessSourceMethod_IsInstalledInstruction_Injected()
        {
            var testMethod = GetMethodInfo(nameof(TestClass.TestMethod));
            var innerMethod = GetMethodInfo(nameof(TestClass.SimpleMethod));
            var mock = new Mock<IMock>();
            mock.Setup(m => m.IsSourceInstruction(It.IsAny<MethodDefinition>(),
                It.Is<Instruction>(cmd => Equivalent(cmd.Operand, innerMethod)))).Returns(true);

            _fakeProcessor.ProcessMethod(new[] { mock.Object }, testMethod);

            mock.Verify(m => m.Inject(It.IsAny<IEmitter>(), It.Is<Instruction>(cmd => Equivalent(cmd.Operand, innerMethod))));
        }

        [Fact]
        public void ProcessSourceMethod_IsAsyncMethod_RecursivelyCallsWithBody()
        {
            var asyncMethod = GetMethodInfo(nameof(TestClass.AsyncMethod));
            var innerMethod = GetMethodInfo(nameof(TestClass.SimpleMethod));
            var mock = new Mock<IMock>();

            _fakeProcessor.ProcessMethod(new[] { mock.Object }, asyncMethod);

            mock.Verify(m => m.IsSourceInstruction(It.IsAny<MethodDefinition>(),
                It.Is<Instruction>(cmd => Equivalent(cmd.Operand, innerMethod))));
        }

        [Fact]
        internal void ProcessSourceMethod_MembersFromTheSameModule_Success()
        {
            _fakeProcessor.ProcessMethod(new[] { Mock.Of<IMock>() }, GetMethodInfo(nameof(TestClass.GetTestClass)));

            var method = _typeInfo.GetMethods(m => m.Name == nameof(TestClass.GetTestClass)).Single();
            Assert.Equal("AutoFake.UnitTests.dll", method.ReturnType.Module.Name);
            Assert.All(method.Parameters, p => Assert.Equal("AutoFake.UnitTests.dll", p.ParameterType.Module.Name));
            Assert.Equal("AutoFake.UnitTests.dll", method.Parameters[1].ParameterType.Module.Name);
        }

        [Fact]
        internal void ProcessSourceMethod_CtorFromTheSameModule_Success()
        {
            _fakeProcessor.ProcessMethod(new[] { Mock.Of<IMock>() }, typeof(TestClass).GetConstructors().Single());

            var method = _typeInfo.GetMethods(m => m.Name == ".ctor").Single();
            Assert.Equal("AutoFake.UnitTests.dll", method.DeclaringType.Module.Name);
        }

        [Theory, AutoMoqData]
        internal void ProcessSourceMethod_MethodWithoutBody_Throws(
	        [Frozen] Mock<ITypeInfo> typeInfo,
	        FakeProcessor generator)
        {
	        typeInfo.Setup(t => t.GetMethod(It.IsAny<MethodReference>())).Returns((MethodDefinition)null);

            Assert.Throws<InvalidOperationException>(() => 
	            generator.ProcessMethod(new[] { Mock.Of<IMock>() }, GetMethodInfo(nameof(TestClass.GetType))));
        }

        [Fact]
        public void ProcessSourceMethod_NullMethod_Throws()
        {
	        Assert.Throws<InvalidOperationException>(() => _fakeProcessor.ProcessMethod(new[] { Mock.Of<IMock>() },
		        GetMethodInfo(nameof(TestClass.GetType))));
        }

        [Fact]
        public void ProcessSourceMethod_MethodWhichCallsMethodWithoutBody_DoesNotThrow()
        {
            _fakeProcessor.ProcessMethod(new[] { Mock.Of<IMock>() },
                GetMethodInfo(nameof(TestClass.MethodWithGetType)));
        }

        [Theory(Skip = "https://github.com/Serg046/AutoFake/issues/145"), AutoMoqData]
        internal void ProcessSourceMethod_MethodWhichCallsNullMethod_DoesNotThrow(
	        [Frozen, InjectModule] Mock<ITypeInfo> typeInfo,
            [Frozen] FakeOptions options,
	        FakeProcessor generator)
        {
	        options.IncludeAllVirtualMembers = true;
	        typeInfo.Setup(t => t.GetDerivedVirtualMethods(It.IsAny<MethodDefinition>()))
		        .Returns(new MethodDefinition[] {null});
	        var typeInfoImp = new TypeInfo(typeof(object), new List<FakeDependency>(), new FakeOptions());
	        typeInfo.Setup(t => t.GetMethod(It.IsAny<MethodReference>()))
		        .Returns(typeInfoImp.GetMethods(m => m.Name == nameof(ToString)).Single);

            Action act = () => generator.ProcessMethod(new[] {Mock.Of<IMock>()},
		        typeof(object).GetMethod(nameof(ToString)));

            act.Should().NotThrow();
        }

        [Fact]
        public void ProcessSourceMethod_RecursiveMethod_Success()
        {
            var typeInfo = new TypeInfo(typeof(object), new List<FakeDependency>(), new FakeOptions());
            var gen = new FakeProcessor(typeInfo, new FakeOptions());
            var method = typeof(object).GetMethod(nameof(ToString));

            gen.ProcessMethod(new []{Mock.Of<IMock>()}, method);
        }

        [AutoMoqData, Theory(Skip = "https://github.com/Serg046/AutoFake/issues/145")]
        internal void ProcessSourceMethod_VirtualMethodWithSpecification_Success([InjectModule]Mock<ITypeInfo> typeInfo)
        {
	        var typeInfoImp = new TypeInfo(typeof(Stream), new List<FakeDependency>(), new FakeOptions());
	        var method = typeof(Stream).GetMethod(nameof(Stream.WriteByte));
	        var methodDef = typeInfoImp.GetMethods(m => m.Name == method.Name).Single();
	        typeInfo.Setup(t => t.GetMethod(It.IsAny<MethodReference>())).Returns(methodDef);
	        typeInfo.Setup(t => t.GetDerivedVirtualMethods(It.IsAny<MethodDefinition>()))
		        .Returns(typeInfoImp.GetDerivedVirtualMethods(methodDef));
	        var gen = new FakeProcessor(typeInfo.Object, new FakeOptions
	        {
		        VirtualMembers = { nameof(Stream.WriteByte) }
	        });

	        gen.ProcessMethod(new[] { Mock.Of<IMock>() }, method);

			typeInfo.Verify(t => t.GetDerivedVirtualMethods(It.Is<MethodDefinition>(
				m => m.Name == method.Name && method.DeclaringType.FullName == "System.IO.Stream")));
			typeInfo.Verify(t => t.GetDerivedVirtualMethods(It.Is<MethodDefinition>(
				m => m.Name == method.Name && m.DeclaringType.FullName == "System.IO.MemoryStream")));
		}

        [AutoMoqData, Theory(Skip = "https://github.com/Serg046/AutoFake/issues/145")]
        internal void ProcessSourceMethod_VirtualMethodWithAllEnabled_Success([InjectModule]Mock<ITypeInfo> typeInfo)
        {
	        var typeInfoImp = new TypeInfo(typeof(Stream), new List<FakeDependency>(), new FakeOptions(){Debug = false});
	        var method = typeof(Stream).GetMethod(nameof(Stream.WriteByte));
	        var methodDef = typeInfoImp.GetMethods(m => m.Name == method.Name).Single();
	        typeInfo.Setup(t => t.GetMethod(It.IsAny<MethodReference>())).Returns(methodDef);
	        typeInfo.Setup(t => t.GetDerivedVirtualMethods(It.IsAny<MethodDefinition>()))
		        .Returns(typeInfoImp.GetDerivedVirtualMethods(methodDef));
	        var gen = new FakeProcessor(typeInfo.Object, new FakeOptions
	        {
		        IncludeAllVirtualMembers = true
	        });

	        gen.ProcessMethod(new[] { Mock.Of<IMock>() }, method);

	        typeInfo.Verify(t => t.GetDerivedVirtualMethods(It.Is<MethodDefinition>(
		        m => m.Name == method.Name && method.DeclaringType.FullName == "System.IO.Stream")));
			typeInfo.Verify(t => t.GetDerivedVirtualMethods(It.Is<MethodDefinition>(
				m => m.Name == method.Name && m.DeclaringType.FullName == "System.IO.MemoryStream")));
		}

        [Theory]
		[InlineAutoMoqData(AnalysisLevels.Type, "Type1", "Type1", "Asm1", "Asm1", true)]
		[InlineAutoMoqData(AnalysisLevels.Type, "Type1", "Type2", "Asm1", "Asm1", false)]
		[InlineAutoMoqData(AnalysisLevels.Assembly, "Type1", "Type1", "Asm1", "Asm1", true)]
		[InlineAutoMoqData(AnalysisLevels.Assembly, "Type1", "Type1", "Asm1", "Asm2", false)]
		[InlineAutoMoqData(AnalysisLevels.AllAssemblies, "Type1", "Type1", "Asm1", "Asm1", true)]
		[InlineAutoMoqData(AnalysisLevels.AllAssemblies, "Type1", "Type2", "Asm1", "Asm1", true)]
		[InlineAutoMoqData(AnalysisLevels.AllAssemblies, "Type1", "Type1", "Asm1", "Asm2", true)]
		[InlineAutoMoqData(AnalysisLevels.AllAssemblies, "Type1", "Type2", "Asm1", "Asm2", true)]
		internal void ProcessSourceMethod_DifferentAnalysisLevels_Success(
	        AnalysisLevels analysisLevel,
            string type1, string type2,
            string asm1, string asm2,
            bool injected,
            [Frozen] FakeOptions options,
	        [Frozen] MethodDefinition executeMethod,
            Mock<IMock> mock,
            MethodInfo methodInfo,
	        FakeProcessor fakeProcessor)
        {
            // Arrange
	        ModuleDefinition module1, module2 = null;
	        if (asm1 == asm2)
	        {
		        module1 = module2 = ModuleDefinition.CreateModule(asm1, ModuleKind.Dll);
	        }
	        else
	        {
		        module1 = ModuleDefinition.CreateModule(asm1, ModuleKind.Dll);
		        module2 = ModuleDefinition.CreateModule(asm2, ModuleKind.Dll);
            }

            options.AnalysisLevel = analysisLevel;
            var method = new MethodDefinition("WrapperMethod", MethodAttributes.Public, new TypeReference("Ns", type2, module2, null));
            var innerMethod = new MethodDefinition("MockedMethod", MethodAttributes.Public, new TypeReference("Ns", type2, module2, null));
            SetType(executeMethod, type1, module1);
            SetType(method, type2, module2);
            SetType(innerMethod, type2, module2);
            executeMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Call, method));
            var instruction = Instruction.Create(OpCodes.Call, innerMethod);
            var copy = instruction.Copy();
            method.Body.Instructions.Add(instruction);
            mock.Setup(m => m.IsSourceInstruction(It.IsAny<MethodDefinition>(), It.IsAny<Instruction>())).Returns(false);
            mock.Setup(m => m.IsSourceInstruction(executeMethod, instruction)).Returns(true);

            // Act
            fakeProcessor.ProcessMethod(new[] { mock.Object }, methodInfo);

            // Assert
            mock.Verify(m => m.Inject(
		            It.IsAny<IEmitter>(),
		            It.Is<Instruction>(cmd => cmd.OpCode == copy.OpCode && cmd.Operand == copy.Operand)),
	            injected ? Times.Once() : Times.Never());
        }

        [Theory(Skip = "https://github.com/Serg046/AutoFake/issues/145"), AutoMoqData]
        internal void ProcessSourceMethod_UnsupportedAnalysisLevel_Success(
            [Frozen] FakeOptions options,
            [Frozen] MethodDefinition executeMethod,
            MethodInfo methodInfo,
            FakeProcessor fakeProcessor)
        {
            options.AnalysisLevel = (AnalysisLevels)100;
            executeMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Call, executeMethod));

            Action act = () => fakeProcessor.ProcessMethod(new[] { Mock.Of<IMock>()}, methodInfo);

            act.Should().Throw<NotSupportedException>().WithMessage("100 is not supported");
        }

        [Theory]
		[InlineAutoMoqData(1, true)]
		[InlineAutoMoqData(2, false)]
        internal void ProcessSourceMethod_CustomAssembly_Success(
            int asmVersion, bool injected,
            [Frozen] FakeOptions options,
            [Frozen] MethodDefinition executeMethod,
            Mock<IMock> mock,
            MethodInfo methodInfo,
            FakeProcessor fakeProcessor)
        {
            // Arrange
            var assembly = Assembly.GetExecutingAssembly();
            var type1 = "Type1";
            var type2 = "Type2";
            var module1 = ModuleDefinition.CreateModule("asm1", ModuleKind.Dll);
            var module2 = ModuleDefinition.CreateModule(assembly.GetName().Name, ModuleKind.Dll);
            module2.Assembly.Name.Version = new Version(asmVersion, 0);

            options.AnalysisLevel = AnalysisLevels.Type;
            options.Assemblies.Add(assembly);
            SetType(executeMethod, type1, module1);
            var method = new MethodDefinition("WrapperMethod", MethodAttributes.Public, new TypeReference("Ns", type2, module2, null));
            SetType(method, type2, module2);
            executeMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Call, method));
            var innerMethod = new MethodDefinition("MockedMethod", MethodAttributes.Public, new TypeReference("Ns", type2, module2, null));
            SetType(innerMethod, type2, module2);
            var instruction = Instruction.Create(OpCodes.Call, innerMethod);
            var copy = instruction.Copy();
            method.Body.Instructions.Add(instruction);
            mock.Setup(m => m.IsSourceInstruction(It.IsAny<MethodDefinition>(), It.IsAny<Instruction>())).Returns(false);
            mock.Setup(m => m.IsSourceInstruction(executeMethod, instruction)).Returns(true);

            // Act
            fakeProcessor.ProcessMethod(new[] { mock.Object }, methodInfo);

            // Assert
            mock.Verify(m => m.Inject(
		            It.IsAny<IEmitter>(),
		            It.Is<Instruction>(cmd => cmd.OpCode == copy.OpCode && cmd.Operand == copy.Operand)),
	            injected ? Times.Once() : Times.Never());
        }

        private bool Equivalent(object operand, MethodInfo innerMethod) => 
            operand is MethodReference method &&
            method.Name == innerMethod.Name &&
            method.Parameters.Count == innerMethod.GetParameters().Length;

        public static IEnumerable<object[]> GetCallbackFieldTestData()
        {
            yield return new object[] {null, false};
            yield return new object[] {new Action(() => Console.WriteLine(0)), true};
        }

        private MethodInfo GetMethodInfo(string name) => typeof(TestClass).GetMethod(name);

        private void SetType(MethodDefinition methodToUpdate, string typeName, ModuleDefinition moduleToSet)
        {
	        var typeDef = new TypeDefinition("Ns", typeName, TypeAttributes.Class);
	        var field = typeDef.GetType().GetField("module", BindingFlags.Instance | BindingFlags.NonPublic);
	        field.SetValue(typeDef, moduleToSet);
	        methodToUpdate.DeclaringType = typeDef;
        }

        private class TypeDefHelper
        {
	        public CustomTypeDefinition GeTypeDefinition(string ns, string name)
		        => new CustomTypeDefinition(ns, name, null, null);
        }

        public class CustomTypeDefinition : TypeReference
        {
	        protected CustomTypeDefinition(string @namespace, string name) : base(@namespace, name)
	        {
	        }

	        public CustomTypeDefinition(string @namespace, string name, ModuleDefinition module, IMetadataScope scope) : base(@namespace, name, module, scope)
	        {
	        }

	        public CustomTypeDefinition(string @namespace, string name, ModuleDefinition module, IMetadataScope scope, bool valueType) : base(@namespace, name, module, scope, valueType)
	        {
	        }
        }

        private class TestClass
        {
            public void SimpleMethod()
            {
                var a = 5;
                var b = a;
            }

            public void TestMethod()
            {
                SimpleMethod();
            }

            public Type MethodWithGetType() => GetType();

            public async void AsyncMethod()
            {
                await Task.Delay(1);
                SimpleMethod();
            }

            public TestClass GetTestClass(TestClass testClass, int x) => testClass;
        }
    }
}
