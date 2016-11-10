using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AutoFake.Exceptions;
using AutoFake.Setup;
using GuardExtensions;
using Mono.Cecil;
using Xunit;
using FieldAttributes = Mono.Cecil.FieldAttributes;

namespace AutoFake.UnitTests
{
    public class ExecutorTests
    {
        public void SomeVoidMethod() { }
        public int SomeMethod(int a) => a;
        public object SomeMethodWithObjectArg(object a) => a;
        public int SomeProperty => 1;
        private static readonly List<int> _actualCallsField = new List<int>() { 0 };

        public void TestMethod()
        {
            SomeMethod(1);
            SomeMethodWithObjectArg(null);
        }

        private FakeGenerator GetFakeGenerator()
        {
            var typeInfo = new TypeInfo(GetType(), new List<FakeDependency>());
            return new FakeGenerator(typeInfo, new MockerFactory(), new GeneratedObject());
        }

        [Fact]
        public void Ctor_Null_Throws()
        {
            Expression<Action> expr = () => TestMethod();
            Assert.Throws<ContractFailedException>(() => new Executor(null, null));
            Assert.Throws<ContractFailedException>(() => new Executor(new GeneratedObject(), null));
            Assert.Throws<ContractFailedException>(() => new Executor(null, expr));
        }

        [Fact]
        public void Execute_IncorrectField_Throws()
        {
            var mockedMemberInfo = new MockedMemberInfo(new FakeSetupPack(), GetType().GetMethods().First(), null);
            mockedMemberInfo.RetValueField = new FieldDefinition("Test", FieldAttributes.Public, new FunctionPointerType());

            var generateObject = new GeneratedObject();
            generateObject.Type = GetType();
            generateObject.MockedMembers.Add(mockedMemberInfo);

            Expression<Action> expr = () => TestMethod();
            Assert.Throws<FakeGeneretingException>(() => new Executor(generateObject, expr).Execute());
        }

        private static FakeArgument GetFakeArgument(dynamic value)
            => new FakeArgument(new EqualityArgumentChecker(value));

        [Fact]
        public void Execute_ValidInput_RetFieldsAreSet()
        {
            var setupCollection = new SetupCollection();

            setupCollection.Add(new FakeSetupPack()
            {
                Method = GetType().GetMethod(nameof(SomeVoidMethod)),
                IsVoid = true,
                ReturnObjectFieldName = nameof(SomeVoidMethod)
            });

            setupCollection.Add(new FakeSetupPack()
            {
                Method = GetType().GetMethod(nameof(SomeMethod)),
                SetupArguments = new List<FakeArgument>() { GetFakeArgument(1) },
                ReturnObject = 7,
                ReturnObjectFieldName = nameof(SomeMethod),
                IsReturnObjectSet = true
            });

            setupCollection.Add(new FakeSetupPack()
            {
                Method = GetType().GetProperty(nameof(SomeProperty)).GetMethod,
                ReturnObject = 7,
                ReturnObjectFieldName = nameof(SomeProperty),
                IsReturnObjectSet = true
            });

            var generateObject = GetFakeGenerator().Generate(setupCollection, GetType().GetMethod(nameof(TestMethod)));

            //act
            Expression<Action> expr = () => TestMethod();
            new Executor(generateObject, expr).Execute();

            //assert
            foreach (var mockedMemberInfo in generateObject.MockedMembers.Where(m => !m.Setup.IsVoid))
            {
                var field = generateObject.Type
                    .GetField(mockedMemberInfo.RetValueField.Name, BindingFlags.NonPublic | BindingFlags.Static);

                Assert.Equal(7, field.GetValue(null));
            }
        }

        [Fact]
        public void Execute_ActualCallsFieldIsMissed_Throws()
        {
            var setupCollection = new SetupCollection();

            setupCollection.Add(new FakeSetupPack()
            {
                Method = GetType().GetMethod(nameof(SomeMethod)),
                ReturnObjectFieldName = nameof(SomeMethod),
                SetupArguments = new List<FakeArgument>() { GetFakeArgument(1) },
                NeedCheckArguments = true,
                IsReturnObjectSet = true
            });

            var generateObject = GetFakeGenerator().Generate(setupCollection, GetType().GetMethod(nameof(TestMethod)));
            generateObject.MockedMembers[0].ActualCallsField =
                new FieldDefinition("Test", FieldAttributes.Public, new FunctionPointerType());

            Expression<Action> expr = () => TestMethod();
            Assert.Throws<FakeGeneretingException>(() => new Executor(generateObject, expr).Execute());
        }

        [Theory]
        [InlineData(2, true)]
        [InlineData(1, false)]
        public void Execute_InvalidSetupForArguments_Throws(object argument, bool shoudBeFailed)
        {
            var setupCollection = new SetupCollection();

            setupCollection.Add(new FakeSetupPack()
            {
                Method = GetType().GetMethod(nameof(SomeMethod)),
                ReturnObjectFieldName = nameof(SomeMethod),
                SetupArguments = new List<FakeArgument> { GetFakeArgument(argument) },
                NeedCheckArguments = true,
                IsReturnObjectSet = true
            });

            var generateObject = GetFakeGenerator().Generate(setupCollection, GetType().GetMethod(nameof(TestMethod)));
            generateObject.MockedMembers[0].ActualCallsField =
                new FieldDefinition(nameof(_actualCallsField), FieldAttributes.Public, new FunctionPointerType());

            Expression<Action> expr = () => TestMethod();
            if (shoudBeFailed)
                Assert.Throws<VerifiableException>(() => new Executor(generateObject, expr).Execute());
            else
                new Executor(generateObject, expr).Execute();
        }

        [Fact]
        public void Execute_NullArgument_Success()
        {
            var setupCollection = new SetupCollection();

            setupCollection.Add(new FakeSetupPack()
            {
                Method = GetType().GetMethod(nameof(SomeMethodWithObjectArg)),
                ReturnObjectFieldName = nameof(SomeMethodWithObjectArg),
                SetupArguments = new List<FakeArgument>() { GetFakeArgument(null) },
                NeedCheckArguments = true,
                IsReturnObjectSet = true
            });

            var generateObject = GetFakeGenerator().Generate(setupCollection, GetType().GetMethod(nameof(TestMethod)));
            generateObject.MockedMembers[0].ActualCallsField =
                new FieldDefinition(nameof(_actualCallsField), FieldAttributes.Public, new FunctionPointerType());

            Expression<Action> expr = () => TestMethod();
            new Executor(generateObject, expr).Execute();
        }

        [Theory]
        [InlineData(2, true)]
        [InlineData(1, false)]
        public void Execute_InvalidSetupForExpectedCalls_Throws(int expectedCalls, bool shoudBeFailed)
        {
            var setupCollection = new SetupCollection();

            setupCollection.Add(new FakeSetupPack()
            {
                Method = GetType().GetMethod(nameof(SomeMethod)),
                ReturnObjectFieldName = nameof(SomeMethod),
                NeedCheckCallsCount = true,
                SetupArguments = new List<FakeArgument>() { GetFakeArgument(1) },
                ExpectedCallsCountFunc = i => i == expectedCalls,
                IsReturnObjectSet = true
            });

            var generateObject = GetFakeGenerator().Generate(setupCollection, GetType().GetMethod(nameof(TestMethod)));
            generateObject.MockedMembers[0].ActualCallsField =
                new FieldDefinition(nameof(_actualCallsField), FieldAttributes.Public, new FunctionPointerType());

            Expression<Action> expr = () => TestMethod();
            if (shoudBeFailed)
                Assert.Throws<ExpectedCallsException>(() => new Executor(generateObject, expr).Execute());
            else
                new Executor(generateObject, expr).Execute();
        }
    }
}
