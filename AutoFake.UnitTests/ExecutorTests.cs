using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AutoFake.Exceptions;
using AutoFake.Setup;
using Mono.Cecil;
using Moq;
using Xunit;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using Mock = AutoFake.Setup.Mock;

namespace AutoFake.UnitTests
{
    public class ExecutorTests
    {
        public void SomeVoidMethod() { }
        public int SomeMethod(int a) => a;
        public object SomeMethodWithObjectArg(object a) => a;
        public int SomeProperty => 1;
        private static readonly List<int> _actualCallsField = new List<int>() { 0 };
        private GeneratedObject _generatedObject;

        public void TestMethod()
        {
            SomeMethod(1);
            SomeMethodWithObjectArg(null);
        }

        private FakeGenerator GetFakeGenerator()
        {
            var typeInfo = new TypeInfo(GetType(), new List<FakeDependency>());
            _generatedObject = new GeneratedObject(typeInfo);
            return new FakeGenerator(typeInfo, new MockerFactory(), _generatedObject);
        }

        [Fact]
        public void Execute_IncorrectField_Throws()
        {
            var mockedMemberInfo = new MockedMemberInfo(new ReplaceableMock(GetSourceMethod(nameof(SomeVoidMethod)), new List<FakeArgument>(),
                new ReplaceableMock.Parameters {ReturnObject = null}), null, null);
            mockedMemberInfo.RetValueField = new FieldDefinition("Test", FieldAttributes.Public, new FunctionPointerType());
            var generateObject = new GeneratedObject(new TypeInfo(GetType(), new List<FakeDependency>()));
            generateObject.MockedMembers.Add(mockedMemberInfo);

            Expression<Action> expr = () => TestMethod();

            Assert.Throws<FakeGeneretingException>(() => new Executor(generateObject, expr).Execute());
        }

        [Fact]
        public void Execute_ValidInput_RetFieldsAreSet()
        {
            var setupCollection = new List<Mock>();

            setupCollection.Add(new ReplaceableMock(GetSourceMethod(nameof(SomeVoidMethod)), new List<FakeArgument>(), new ReplaceableMock.Parameters()));
            setupCollection.Add(new ReplaceableMock(GetSourceMethod(nameof(SomeMethod)), new List<FakeArgument>() { GetFakeArgument(1) },
                new ReplaceableMock.Parameters { ReturnObject = 7 }));
            setupCollection.Add(new ReplaceableMock(GetSourceProperty(nameof(SomeProperty)), new List<FakeArgument>(),
                new ReplaceableMock.Parameters() { ReturnObject = 7 }));
            GetFakeGenerator().Generate(setupCollection, GetType().GetMethod(nameof(TestMethod)));

            //act
            Expression<Action> expr = () => TestMethod();
            new Executor(_generatedObject, expr).Execute();

            //assert
            foreach (var mockedMemberInfo in _generatedObject.MockedMembers.Where(m => m.Mock.SourceMember.ReturnType != typeof(void)))
            {
                var field = _generatedObject.Type
                    .GetField(mockedMemberInfo.RetValueField.Name, BindingFlags.NonPublic | BindingFlags.Static);

                Assert.Equal(7, field.GetValue(null));
            }
        }

        [Fact]
        public void Execute_ActualCallsFieldIsMissed_Throws()
        {
            var mock = new ReplaceableMock(GetSourceMethod(nameof(SomeMethod)), new List<FakeArgument>() { GetFakeArgument(1) },
                new ReplaceableMock.Parameters() { NeedCheckArguments = true, ReturnObject = null});
            GetFakeGenerator().Generate(new Mock[] {mock}, GetType().GetMethod(nameof(TestMethod)));
            _generatedObject.MockedMembers[0].ActualCallsField =
                new FieldDefinition("Test", FieldAttributes.Public, new FunctionPointerType());

            Expression<Action> expr = () => TestMethod();

            Assert.Throws<FakeGeneretingException>(() => new Executor(_generatedObject, expr).Execute());
        }

        [Theory]
        [InlineData(2, true)]
        [InlineData(1, false)]
        public void Execute_InvalidSetupForArguments_Throws(object argument, bool shoudBeFailed)
        {
            var mock = new ReplaceableMock(GetSourceMethod(nameof(SomeMethod)), new List<FakeArgument>() { GetFakeArgument(argument) },
                new ReplaceableMock.Parameters() { NeedCheckArguments = true, ReturnObject = null });
            GetFakeGenerator().Generate(new Mock[] { mock }, GetType().GetMethod(nameof(TestMethod)));
            _generatedObject.MockedMembers[0].ActualCallsField =
                new FieldDefinition(nameof(_actualCallsField), FieldAttributes.Public, new FunctionPointerType());

            Expression<Action> expr = () => TestMethod();

            if (shoudBeFailed)
                Assert.Throws<VerifiableException>(() => new Executor(_generatedObject, expr).Execute());
            else
                new Executor(_generatedObject, expr).Execute();
        }

        [Fact]
        public void Execute_NullArgument_Success()
        {
            var mock = new ReplaceableMock(GetSourceMethod(nameof(SomeMethodWithObjectArg)), new List<FakeArgument>() { GetFakeArgument(null) },
                new ReplaceableMock.Parameters() { NeedCheckArguments = true, ReturnObject = null });
            GetFakeGenerator().Generate(new Mock[] { mock }, GetType().GetMethod(nameof(TestMethod)));
            _generatedObject.MockedMembers[0].ActualCallsField =
                new FieldDefinition(nameof(_actualCallsField), FieldAttributes.Public, new FunctionPointerType());

            Expression<Action> expr = () => TestMethod();

            new Executor(_generatedObject, expr).Execute();
        }

        [Theory]
        [InlineData(2, true)]
        [InlineData(1, false)]
        public void Execute_InvalidSetupForExpectedCalls_Throws(int expectedCalls, bool shoudBeFailed)
        {
            var mock = new ReplaceableMock(GetSourceMethod(nameof(SomeMethod)), new List<FakeArgument>() { GetFakeArgument(1) },
                new ReplaceableMock.Parameters() { ExpectedCallsCountFunc = i => i == expectedCalls, ReturnObject = null });
            GetFakeGenerator().Generate(new Mock[] { mock }, GetType().GetMethod(nameof(TestMethod)));
            _generatedObject.MockedMembers[0].ActualCallsField =
                new FieldDefinition(nameof(_actualCallsField), FieldAttributes.Public, new FunctionPointerType());

            Expression<Action> expr = () => TestMethod();

            if (shoudBeFailed)
                Assert.Throws<ExpectedCallsException>(() => new Executor(_generatedObject, expr).Execute());
            else
                new Executor(_generatedObject, expr).Execute();
        }

        [Fact]
        public void Execute_ValidInput_CallbackIsSet()
        {
            var mock = new ReplaceableMock(GetSourceProperty(nameof(SomeProperty)), new List<FakeArgument>(),
                new ReplaceableMock.Parameters() { ReturnObject = 7, Callback = () => { throw new InvalidOperationException(); } });
            GetFakeGenerator().Generate(new Mock[] { mock }, GetType().GetMethod(nameof(TestMethod)));

            //act
            Expression<Action> expr = () => TestMethod();
            new Executor(_generatedObject, expr).Execute();

            //assert
            var mockedMemberInfo = _generatedObject.MockedMembers.Single();
            var field = _generatedObject.Type
                .GetField(mockedMemberInfo.CallbackField.Name, BindingFlags.NonPublic | BindingFlags.Static);
            var callback = field.GetValue(null) as Action;
            Assert.Throws<InvalidOperationException>(() => callback());
        }

        private ISourceMember GetSourceMethod(string name)
            => new SourceMethod(GetType().GetMethod(name));

        private ISourceMember GetSourceProperty(string name)
            => new SourceMethod(GetType().GetProperty(name).GetMethod);

        private static FakeArgument GetFakeArgument(dynamic value)
            => new FakeArgument(new EqualityArgumentChecker(value));
    }
}
