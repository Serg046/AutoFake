using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using AutoFake.Expression;
using AutoFake.Setup.Mocks;
using AutoFixture.Xunit2;
using FluentAssertions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Moq;
using Xunit;
using InvocationExpression = AutoFake.Expression.InvocationExpression;
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
            Expression<Action<TestClass>> expr = t => t.SimpleMethod();
            var mock = new Mock<IMock>();

            _fakeProcessor.ProcessMethod(new[] { mock.Object }, new InvocationExpression(expr));

            mock.Verify(m => m.BeforeInjection(It.Is<MethodDefinition>(met => met.Name == nameof(TestClass.SimpleMethod))));
        }

        [Fact]
        public void ProcessSourceMethod_IsInstalledInstruction_Injected()
        {
            Expression<Action<TestClass>> expr = t => t.TestMethod();
            var innerMethod = GetMethodInfo(nameof(TestClass.SimpleMethod));
            var mock = new Mock<IMock>();
            mock.Setup(m => m.IsSourceInstruction(It.IsAny<MethodDefinition>(),
                It.Is<Instruction>(cmd => Equivalent(cmd.Operand, innerMethod)), It.IsAny<IEnumerable<GenericArgument>>())).Returns(true);

            _fakeProcessor.ProcessMethod(new[] { mock.Object }, new InvocationExpression(expr));

            mock.Verify(m => m.Inject(It.IsAny<IEmitter>(), It.Is<Instruction>(cmd => Equivalent(cmd.Operand, innerMethod))));
        }

        [Fact]
        public void ProcessSourceMethod_IsAsyncMethod_RecursivelyCallsWithBody()
        {
            Expression<Action<TestClass>> expr = t => t.AsyncMethod();
            var innerMethod = GetMethodInfo(nameof(TestClass.SimpleMethod));
            var mock = new Mock<IMock>();

            _fakeProcessor.ProcessMethod(new[] { mock.Object }, new InvocationExpression(expr));

            mock.Verify(m => m.IsSourceInstruction(It.IsAny<MethodDefinition>(),
                It.Is<Instruction>(cmd => Equivalent(cmd.Operand, innerMethod)), It.IsAny<IEnumerable<GenericArgument>>()));
        }

        [Fact]
        internal void ProcessSourceMethod_MembersFromTheSameModule_Success()
        {
            Expression<Action<TestClass>> expr = t => t.GetTestClass(Arg.IsAny<TestClass>(), Arg.IsAny<int>());

            _fakeProcessor.ProcessMethod(new[] { Mock.Of<IMock>() }, new InvocationExpression(expr));

            var method = _typeInfo.GetMethods(m => m.Name == nameof(TestClass.GetTestClass)).Single();
            Assert.Equal("AutoFake.UnitTests.dll", method.ReturnType.Module.Name);
            Assert.All(method.Parameters, p => Assert.Equal("AutoFake.UnitTests.dll", p.ParameterType.Module.Name));
            Assert.Equal("AutoFake.UnitTests.dll", method.Parameters[1].ParameterType.Module.Name);
        }

        [Fact]
        internal void ProcessSourceMethod_CtorFromTheSameModule_Success()
        {
	        Expression<Action> expr = () => new TestClass();

            _fakeProcessor.ProcessMethod(new[] { Mock.Of<IMock>() }, new InvocationExpression(expr));

            var method = _typeInfo.GetMethods(m => m.Name == ".ctor").Single();
            Assert.Equal("AutoFake.UnitTests.dll", method.DeclaringType.Module.Name);
        }

        [Theory, AutoMoqData]
        internal void ProcessSourceMethod_MethodWithoutBody_Throws(
	        [Frozen] Mock<ITypeInfo> typeInfo,
	        FakeProcessor generator)
        {
            Expression<Action<TestClass>> expr = t => t.GetType();
	        typeInfo.Setup(t => t.GetMethod(It.IsAny<MethodReference>(), true)).Returns((MethodDefinition)null);

            Assert.Throws<InvalidOperationException>(() => 
	            generator.ProcessMethod(new[] { Mock.Of<IMock>() }, new InvocationExpression(expr)));
        }

        [Fact]
        public void ProcessSourceMethod_NullMethod_Throws()
        {
            Expression<Action<TestClass>> expr = t => t.GetType();

	        Assert.Throws<InvalidOperationException>(() => _fakeProcessor.ProcessMethod(new[] { Mock.Of<IMock>() },
		        new InvocationExpression(expr)));
        }

        [Fact]
        public void ProcessSourceMethod_MethodWhichCallsMethodWithoutBody_DoesNotThrow()
        {
            Expression<Action<TestClass>> expr = t => t.MethodWithGetType();

            _fakeProcessor.ProcessMethod(new[] { Mock.Of<IMock>() }, new InvocationExpression(expr));
        }
        
        [Fact]
        public void ProcessSourceMethod_RecursiveMethod_Success()
        {
            var typeInfo = new TypeInfo(typeof(object), new List<FakeDependency>(), new FakeOptions());
            var gen = new FakeProcessor(typeInfo, new FakeOptions {DisableVirtualMembers = true});
            Expression<Action<TestClass>> expr = t => t.ToString();

            gen.ProcessMethod(new []{Mock.Of<IMock>()}, new InvocationExpression(expr));
        }

        [AutoMoqData, Theory]
        internal void ProcessSourceMethod_VirtualMethodWithSpecification_Success([InjectModule]Mock<ITypeInfo> typeInfo)
        {
	        var typeInfoImp = new TypeInfo(typeof(Stream), new List<FakeDependency>(), new FakeOptions());
	        var method = typeof(Stream).GetMethod(nameof(Stream.WriteByte));
	        var methodDef = typeInfoImp.GetMethods(m => m.Name == method.Name).Single();
	        typeInfo.Setup(t => t.GetMethod(It.IsAny<MethodReference>(), It.IsAny<bool>())).Returns(methodDef);
	        typeInfo.Setup(t => t.GetMethods(It.IsAny<Predicate<MethodDefinition>>()))
		        .Returns((Predicate<MethodDefinition> p) => typeInfoImp.GetMethods(p));
	        typeInfo.Setup(t => t.GetAllImplementations(It.IsAny<MethodDefinition>(), true))
		        .Returns((MethodDefinition m, bool _) => typeInfoImp.GetAllImplementations(m));
	        var gen = new FakeProcessor(typeInfo.Object, new FakeOptions
	        {
		        Debug = DebugMode.Disabled,
		        AllowedVirtualMembers =
		        {
			        m => m.Name == nameof(Stream.WriteByte) &&
			             (m.DeclaringType is "System.IO.Stream" or "System.IO.MemoryStream")
		        }
	        });
	        Expression<Action<Stream>> expr = t => t.WriteByte(Arg.IsAny<byte>());


            gen.ProcessMethod(new[] { Mock.Of<IMock>() }, new InvocationExpression(expr));

			typeInfo.Verify(t => t.GetAllImplementations(It.Is<MethodDefinition>(
				m => m.Name == method.Name && method.DeclaringType.FullName == "System.IO.Stream"), true));
		}

        [Theory]
		[InlineAutoMoqData(AnalysisLevels.Type, "Type1", "Type1", "Asm1", "Asm1", true)]
		[InlineAutoMoqData(AnalysisLevels.Type, "Type1", "Type2", "Asm1", "Asm1", false)]
		[InlineAutoMoqData(AnalysisLevels.Assembly, "Type1", "Type1", "Asm1", "Asm1", true)]
		[InlineAutoMoqData(AnalysisLevels.Assembly, "Type1", "Type1", "Asm1", "Asm2", false)]
		[InlineAutoMoqData(AnalysisLevels.AllExceptSystemAndMicrosoft, "Type1", "Type1", "Asm1", "Asm1", true)]
		[InlineAutoMoqData(AnalysisLevels.AllExceptSystemAndMicrosoft, "Type1", "Type2", "Asm1", "Asm1", true)]
		[InlineAutoMoqData(AnalysisLevels.AllExceptSystemAndMicrosoft, "Type1", "Type1", "Asm1", "Asm2", true)]
		[InlineAutoMoqData(AnalysisLevels.AllExceptSystemAndMicrosoft, "Type1", "Type2", "Asm1", "Asm2", true)]
		internal void ProcessSourceMethod_DifferentAnalysisLevels_Success(
	        AnalysisLevels analysisLevel,
            string type1, string type2,
            string asm1, string asm2,
            bool injected,
            [Frozen] FakeOptions options,
	        [Frozen] MethodDefinition executeMethod,
            [Frozen] Mock<ITypeInfo> typeInfo,
            Mock<IMock> mock,
	        Mock<IInvocationExpression> invocationExpression,
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
            mock.Setup(m => m.IsSourceInstruction(It.IsAny<MethodDefinition>(), It.IsAny<Instruction>(), It.IsAny<IEnumerable<GenericArgument>>())).Returns(false);
            mock.Setup(m => m.IsSourceInstruction(executeMethod, instruction, It.IsAny<IEnumerable<GenericArgument>>())).Returns(true);
            typeInfo.Setup(t => t.IsInReferencedAssembly(It.IsAny<AssemblyDefinition>())).Returns(false);
            invocationExpression.Setup(s => s.AcceptMemberVisitor(It.IsAny<IMemberVisitor>()))
	            .Callback((IMemberVisitor v) => v.Visit(null, methodInfo));

            // Act
            fakeProcessor.ProcessMethod(new[] { mock.Object }, invocationExpression.Object);

            // Assert
            mock.Verify(m => m.Inject(
		            It.IsAny<IEmitter>(),
		            It.Is<Instruction>(cmd => cmd.OpCode == copy.OpCode && cmd.Operand == copy.Operand)),
	            injected ? Times.Once() : Times.Never());
        }

        [Theory, AutoMoqData]
        internal void ProcessSourceMethod_UnsupportedAnalysisLevel_Success(
            [Frozen] FakeOptions options,
            [Frozen] MethodDefinition executeMethod,
            MethodInfo methodInfo,
            Mock<IInvocationExpression> invocationExpression,
            FakeProcessor fakeProcessor)
        {
            invocationExpression.Setup(s => s.AcceptMemberVisitor(It.IsAny<IMemberVisitor>()))
	            .Callback((IMemberVisitor v) => v.Visit(null, methodInfo));
            options.AnalysisLevel = (AnalysisLevels)100;
            executeMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Call, executeMethod));

            Action act = () => fakeProcessor.ProcessMethod(new[] { Mock.Of<IMock>()}, invocationExpression.Object);

            act.Should().Throw<NotSupportedException>().WithMessage("100 is not supported");
        }

        [Theory]
		[InlineAutoMoqData(1, true)]
		[InlineAutoMoqData(2, false)]
        internal void ProcessSourceMethod_CustomAssembly_Success(
            int asmVersion, bool injected,
            [Frozen] FakeOptions options,
            [Frozen] MethodDefinition executeMethod,
            [Frozen] Mock<ITypeInfo> typeInfo,
            Mock<IMock> mock,
            MethodInfo methodInfo,
            Mock<IInvocationExpression> invocationExpression,
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
            options.AddReference(assembly.ExportedTypes.First());
            SetType(executeMethod, type1, module1);
            var method = new MethodDefinition("WrapperMethod", MethodAttributes.Public, new TypeReference("Ns", type2, module2, null));
            SetType(method, type2, module2);
            executeMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Call, method));
            var innerMethod = new MethodDefinition("MockedMethod", MethodAttributes.Public, new TypeReference("Ns", type2, module2, null));
            SetType(innerMethod, type2, module2);
            var instruction = Instruction.Create(OpCodes.Call, innerMethod);
            var copy = instruction.Copy();
            method.Body.Instructions.Add(instruction);
            mock.Setup(m => m.IsSourceInstruction(It.IsAny<MethodDefinition>(), It.IsAny<Instruction>(), It.IsAny<IEnumerable<GenericArgument>>())).Returns(false);
            mock.Setup(m => m.IsSourceInstruction(executeMethod, instruction, It.IsAny<IEnumerable<GenericArgument>>())).Returns(true);
            typeInfo.Setup(t => t.IsInReferencedAssembly(It.IsAny<AssemblyDefinition>())).Returns(false);
            typeInfo.Setup(t => t.IsInReferencedAssembly(It.Is<AssemblyDefinition>(a => a.FullName == assembly.FullName))).Returns(true);
            invocationExpression.Setup(s => s.AcceptMemberVisitor(It.IsAny<IMemberVisitor>()))
	            .Callback((IMemberVisitor v) => v.Visit(null, methodInfo));

            // Act
            fakeProcessor.ProcessMethod(new[] { mock.Object }, invocationExpression.Object);

            // Assert
            mock.Verify(m => m.Inject(
		            It.IsAny<IEmitter>(),
		            It.Is<Instruction>(cmd => cmd.OpCode == copy.OpCode && cmd.Operand == copy.Operand)),
	            injected ? Times.Once() : Times.Never());
        }

        [Fact]
        internal void ProcessMethod_GenericArgsOnBothLevels_Success()
        {
	        Expression<Action<GenericLevelOne<byte, int>>> expr1 = t => t.Method(4, 4.0);
	        Expression<Action<GenericLevelTwo<string>>> expr2 = t => t.GenericParameters(4, 4.0, "4", 4M);
	        var typeInfo = new TypeInfo(typeof(GenericLevelOne<byte, int>), new List<FakeDependency>(), new FakeOptions());
	        var proc = new FakeProcessor(typeInfo, new FakeOptions());
	        var mock = new FakeMock(new ReplaceMock(new ProcessorFactory(typeInfo), new InvocationExpression(expr2)));

	        proc.ProcessMethod(new[] {mock}, new InvocationExpression(expr1));

	        mock.AtLeastOneSuccess.Should().BeTrue();
        }

        [Fact]
        internal void ProcessMethod_GenericArgsOnSecondLevel_Success()
        {
	        Expression<Action<GenericExecutor>> expr1 = t => t.GetGenericReturn();
	        Expression<Action<GenericLevelTwo<string>>> expr2 = t => t.GenericReturn(4);
	        var typeInfo = new TypeInfo(typeof(GenericExecutor), new List<FakeDependency>(), new FakeOptions());
	        var proc = new FakeProcessor(typeInfo, new FakeOptions());
	        var mock = new FakeMock(new ReplaceMock(new ProcessorFactory(typeInfo), new InvocationExpression(expr2)));

	        proc.ProcessMethod(new[] { mock }, new InvocationExpression(expr1));

	        mock.AtLeastOneSuccess.Should().BeTrue();
        }

        [Fact]
        internal void ProcessMethod_FieldGenericArgs_Success()
        {
	        Expression<Action<GenericExecutor>> expr1 = t => t.GetField();
	        Expression<Func<GenericLevelOne<short, int>, int>> expr2 = t => t.Field;
	        var typeInfo = new TypeInfo(typeof(GenericExecutor), new List<FakeDependency>(), new FakeOptions());
	        var proc = new FakeProcessor(typeInfo, new FakeOptions());
	        var mock = new FakeMock(new ReplaceMock(new ProcessorFactory(typeInfo), new InvocationExpression(expr2)));

	        proc.ProcessMethod(new[] { mock }, new InvocationExpression(expr1));

	        mock.AtLeastOneSuccess.Should().BeTrue();
        }

        [Fact]
        internal void ProcessMethod_NestedMethodGenericArg_Success()
        {
	        Expression<Action<GenericExecutor>> expr1 = t => t.GetNestedGeneric();
	        Expression<Action<GenericLevelTwo<short>>> expr2 = t => t.NestedGeneric<int>(new short[0]);
	        var typeInfo = new TypeInfo(typeof(GenericExecutor), new List<FakeDependency>(), new FakeOptions());
	        var proc = new FakeProcessor(typeInfo, new FakeOptions());
	        var mock = new FakeMock(new ReplaceMock(new ProcessorFactory(typeInfo), new InvocationExpression(expr2)));

	        proc.ProcessMethod(new[] { mock }, new InvocationExpression(expr1));

	        mock.AtLeastOneSuccess.Should().BeTrue();
        }

        [Fact]
        internal void ProcessMethod_NestedFieldGenericArg_Success()
        {
	        Expression<Action<GenericExecutor>> expr1 = t => t.GetNestedGenericField();
	        Expression<Func<GenericLevelTwo<short>, IEnumerable<short>>> expr2 = t => t.NestedGenericField;
	        var typeInfo = new TypeInfo(typeof(GenericExecutor), new List<FakeDependency>(), new FakeOptions());
	        var proc = new FakeProcessor(typeInfo, new FakeOptions());
	        var mock = new FakeMock(new ReplaceMock(new ProcessorFactory(typeInfo), new InvocationExpression(expr2)));

	        proc.ProcessMethod(new[] { mock }, new InvocationExpression(expr1));

	        mock.AtLeastOneSuccess.Should().BeTrue();
        }

        [Fact]
        internal void ProcessMethod_FieldGenericArgsOnBothLevels_Success()
        {
	        Expression<Action<GenericLevelOne<byte, int>>> expr1 = t => t.GetNestedGenericField();
	        Expression<Func<GenericLevelTwo<byte>, IEnumerable<byte>>> expr2 = t => t.NestedGenericField;
	        var typeInfo = new TypeInfo(typeof(GenericLevelOne<byte, int>), new List<FakeDependency>(), new FakeOptions());
	        var proc = new FakeProcessor(typeInfo, new FakeOptions());
	        var mock = new FakeMock(new ReplaceMock(new ProcessorFactory(typeInfo), new InvocationExpression(expr2)));

	        proc.ProcessMethod(new[] { mock }, new InvocationExpression(expr1));

	        mock.AtLeastOneSuccess.Should().BeTrue();
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

        private class FakeMock : IMock
        {
	        private readonly IMock _mock;
	        public FakeMock(IMock mock) => _mock = mock;
	        public bool AtLeastOneSuccess { get; set; }
            public void BeforeInjection(MethodDefinition method) => _mock.BeforeInjection(method);
	        public void Inject(IEmitter emitter, Instruction instruction) => _mock.Inject(emitter, instruction);
	        public void AfterInjection(IEmitter emitter) => _mock.AfterInjection(emitter);
	        public IList<object> Initialize(Type? type) => _mock.Initialize(type);

	        public bool IsSourceInstruction(MethodDefinition method, Instruction instruction, IEnumerable<GenericArgument> genericArguments)
	        {
		        var isSourceInstruction = _mock.IsSourceInstruction(method, instruction, genericArguments);
		        if (isSourceInstruction)
		        {
			        AtLeastOneSuccess = true;
		        }
		        return isSourceInstruction;
	        }
        }

        private class GenericLevelOne<TReturn, TType> where TReturn : new()
        {
	        public TType Field;

	        public TReturn Method<TMet>(TType p1, TMet p2)
	        {
		        new GenericLevelTwo<string>().GenericParameters(p1, p2, "4", 4M);
		        return new ();
	        }

	        public IEnumerable<TReturn> GetNestedGenericField() => new GenericLevelTwo<TReturn>().NestedGenericField;
        }

        private class GenericExecutor
        {
	        public int GetGenericReturn() => new GenericLevelTwo<string>().GenericReturn(4);
	        public int GetField() => new GenericLevelOne<short, int>().Field;
	        public IEnumerable<int> GetNestedGeneric() => new GenericLevelTwo<short>().NestedGeneric<int>(new short[0]);
	        public IEnumerable<short> GetNestedGenericField() => new GenericLevelTwo<short>().NestedGenericField;
        }

        private class GenericLevelTwo<TType>
        {
	        public IEnumerable<TType> NestedGenericField;

	        public void GenericParameters<TTypeBase, TMetBase, TMet>(TTypeBase p1, TMetBase p2, TType p3, TMet p4)
	        {
	        }

	        public TRet GenericReturn<TRet>(TRet ret) => ret;

	        public IEnumerable<TRet> NestedGeneric<TRet>(IEnumerable<TType> param) => Enumerable.Empty<TRet>();
        }
    }
}
