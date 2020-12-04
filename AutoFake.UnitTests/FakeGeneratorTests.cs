using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AutoFake.Setup.Mocks;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Moq;
using Xunit;

namespace AutoFake.UnitTests
{
    public class FakeGeneratorTests
    {
        private readonly FakeGenerator _fakeGenerator;
        private readonly TypeInfo _typeInfo;

        public FakeGeneratorTests()
        {
            _typeInfo = new TypeInfo(typeof(TestClass), new List<FakeDependency>());
            _fakeGenerator = new FakeGenerator(_typeInfo, new FakeOptions());
        }

        [Fact]
        public void Generate_Mock_PreparedForInjecting()
        {
            var testMethod = GetMethodInfo(nameof(TestClass.SimpleMethod));
            var mock = new Mock<IMock>();

            _fakeGenerator.Generate(new[] { mock.Object }, testMethod);

            mock.Verify(m => m.BeforeInjection(It.Is<MethodDefinition>(met => met.Name == testMethod.Name)));
        }

        [Fact]
        public void Generate_IsInstalledInstruction_Injected()
        {
            var testMethod = GetMethodInfo(nameof(TestClass.TestMethod));
            var innerMethod = GetMethodInfo(nameof(TestClass.SimpleMethod));
            var mock = new Mock<IMock>();
            mock.Setup(m => m.IsSourceInstruction(It.IsAny<MethodDefinition>(),
                It.Is<Instruction>(cmd => Equivalent(cmd.Operand, innerMethod)))).Returns(true);

            _fakeGenerator.Generate(new[] { mock.Object }, testMethod);

            mock.Verify(m => m.Inject(It.IsAny<IEmitter>(), It.Is<Instruction>(cmd => Equivalent(cmd.Operand, innerMethod))));
        }

        [Fact]
        public void Generate_IsAsyncMethod_RecursivelyCallsWithBody()
        {
            var asyncMethod = GetMethodInfo(nameof(TestClass.AsyncMethod));
            var innerMethod = GetMethodInfo(nameof(TestClass.SimpleMethod));
            var mock = new Mock<IMock>();

            _fakeGenerator.Generate(new[] { mock.Object }, asyncMethod);

            mock.Verify(m => m.IsSourceInstruction(It.IsAny<MethodDefinition>(),
                It.Is<Instruction>(cmd => Equivalent(cmd.Operand, innerMethod))));
        }

        [Fact]
        internal void Generate_MembersFromTheSameModule_Success()
        {
            _fakeGenerator.Generate(new[] { Mock.Of<IMock>() }, GetMethodInfo(nameof(TestClass.GetTestClass)));

            var method = _typeInfo.Methods.Single(m => m.Name == nameof(TestClass.GetTestClass));
            Assert.Equal("AutoFake.UnitTests.dll", method.ReturnType.Module.Name);
            Assert.All(method.Parameters, p => Assert.Equal("AutoFake.UnitTests.dll", p.ParameterType.Module.Name));
            Assert.Equal("AutoFake.UnitTests.dll", method.Parameters[1].ParameterType.Module.Name);
        }

        [Fact]
        internal void Generate_CtorFromTheSameModule_Success()
        {
            _fakeGenerator.Generate(new[] { Mock.Of<IMock>() }, typeof(TestClass).GetConstructors().Single());

            var method = _typeInfo.Methods.Single(m => m.Name == ".ctor");
            Assert.Equal("AutoFake.UnitTests.dll", method.DeclaringType.Module.Name);
        }

        [Fact]
        public void Generate_MethodWithoutBody_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => _fakeGenerator.Generate(new[] { Mock.Of<IMock>() },
                GetMethodInfo(nameof(TestClass.GetType))));
        }

        [Fact]
        public void Generate_MethodWhichCallsMethodWithoutBody_DoesNotThrow()
        {
            _fakeGenerator.Generate(new[] { Mock.Of<IMock>() },
                GetMethodInfo(nameof(TestClass.MethodWithGetType)));
        }

        [Fact]
        public void Generate_NullMethod_DoesNotThrow()
        {
			var typeInfo = new TypeInfo(typeof(Encoding), new List<FakeDependency>());
			var fakeGenerator = new FakeGenerator(typeInfo, new FakeOptions { IncludeAllVirtualMembers = true });

			fakeGenerator.Generate(new[] {Mock.Of<IMock>()},
				typeof(Encoding).GetMethods()
					.Single(m => m.Name == nameof(Encoding.GetChars) && m.GetParameters().Length == 1));
        }

        [Fact]
        public void Generate_RecursiveMethod_Success()
        {
            var typeInfo = new TypeInfo(typeof(object), new List<FakeDependency>());
            var gen = new FakeGenerator(typeInfo, new FakeOptions());
            var method = typeof(object).GetMethod(nameof(ToString));

            gen.Generate(new []{Mock.Of<IMock>()}, method);
        }

        [AutoMoqData, Theory]
        internal void Generate_VirtualMethodWithSpecification_Success(Mock<ITypeInfo> typeInfo)
        {
	        var method = typeof(Stream).GetMethod(nameof(Stream.WriteByte));
	        var typeInfoImp = new TypeInfo(typeof(Stream), new List<FakeDependency>());
	        typeInfo.Setup(t => t.GetMethod(It.IsAny<MethodReference>()))
		        .Returns(typeInfoImp.Methods.Single(m => m.Name == method.Name));
	        typeInfo.Setup(t => t.GetDerivedVirtualMethods(It.IsAny<MethodDefinition>()))
		        .Returns((MethodDefinition def) => typeInfoImp.GetDerivedVirtualMethods(def));
	        var gen = new FakeGenerator(typeInfo.Object, new FakeOptions
	        {
		        VirtualMembers = { nameof(Stream.WriteByte) }
	        });

	        gen.Generate(new[] { Mock.Of<IMock>() }, method);

			typeInfo.Verify(t => t.GetDerivedVirtualMethods(It.Is<MethodDefinition>(
				m => m.Name == method.Name && method.DeclaringType.FullName == "System.IO.Stream")));
			typeInfo.Verify(t => t.GetDerivedVirtualMethods(It.Is<MethodDefinition>(
				m => m.Name == method.Name && m.DeclaringType.FullName == "System.IO.MemoryStream")));
		}

        [AutoMoqData, Theory]
        internal void Generate_VirtualMethodWithAllEnabled_Success(Mock<ITypeInfo> typeInfo)
        {
	        var method = typeof(Stream).GetMethod(nameof(Stream.WriteByte));
	        var typeInfoImp = new TypeInfo(typeof(Stream), new List<FakeDependency>());
	        typeInfo.Setup(t => t.GetMethod(It.IsAny<MethodReference>()))
		        .Returns(typeInfoImp.Methods.Single(m => m.Name == method.Name));
	        typeInfo.Setup(t => t.GetDerivedVirtualMethods(It.IsAny<MethodDefinition>()))
		        .Returns((MethodDefinition def) => typeInfoImp.GetDerivedVirtualMethods(def));
	        var gen = new FakeGenerator(typeInfo.Object, new FakeOptions
	        {
		        IncludeAllVirtualMembers = true
	        });

	        gen.Generate(new[] { Mock.Of<IMock>() }, method);

	        typeInfo.Verify(t => t.GetDerivedVirtualMethods(It.Is<MethodDefinition>(
		        m => m.Name == method.Name && method.DeclaringType.FullName == "System.IO.Stream")));
	        typeInfo.Verify(t => t.GetDerivedVirtualMethods(It.Is<MethodDefinition>(
		        m => m.Name == method.Name && m.DeclaringType.FullName == "System.IO.MemoryStream")));
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
