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
    public class TestMethodTests
    {
        public void SomeVoidMethod() { }
        public int SomeMethod(int a) => a;
        public int SomeProperty => 1;
        private static List<int> ActualCallsField = new List<int>() {0};

        public void TestMethod()
        {
            SomeMethod(1);
        }

        private readonly FakeGenerator _fakeGenerator;

        public TestMethodTests()
        {
            var typeInfo = new TypeInfo(GetType(), new object[0]);
            _fakeGenerator = new FakeGenerator(typeInfo, new MockerFactory());
        }

        [Fact]
        public void Ctor_Null_Throws()
        {
            Assert.Throws<ContractFailedException>(() => new TestMethod(null));
        }

        [Fact]
        public void Execute_Null_Throws()
        {
            Assert.Throws<ContractFailedException>(() => new TestMethod(new GeneratedObject()).Execute(null));
        }

        [Fact]
        public void Execute_IncorrectField_Throws()
        {
            var mockedMemberInfo = new MockedMemberInfo(new FakeSetupPack());
            mockedMemberInfo.RetValueField = new FieldDefinition("Test", FieldAttributes.Public, new FunctionPointerType());

            var generateObject = new GeneratedObject();
            generateObject.Type = GetType();
            generateObject.MockedMembers = new List<MockedMemberInfo>() { mockedMemberInfo };

            Expression<Action> expr = () => TestMethod();
            Assert.Throws<FakeGeneretingException>(() => new TestMethod(generateObject).Execute(expr));
        }

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
                SetupArguments = new object[] {1},
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

            var generateObject = _fakeGenerator.Generate(setupCollection, GetType().GetMethod(nameof(TestMethod)));

            //act
            Expression<Action> expr = () => TestMethod();
            new TestMethod(generateObject).Execute(expr);

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
                SetupArguments = new object[] {1},
                NeedCheckArguments = true,
                IsReturnObjectSet = true
            });
            
            var generateObject = _fakeGenerator.Generate(setupCollection, GetType().GetMethod(nameof(TestMethod)));
            generateObject.MockedMembers[0].ActualCallsField =
                new FieldDefinition("Test", FieldAttributes.Public, new FunctionPointerType());

            Expression<Action> expr = () => TestMethod();
            Assert.Throws<FakeGeneretingException>(() => new TestMethod(generateObject).Execute(expr));
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
                SetupArguments = new object[] { argument },
                NeedCheckArguments = true,
                IsReturnObjectSet = true
            });

            var generateObject = _fakeGenerator.Generate(setupCollection, GetType().GetMethod(nameof(TestMethod)));
            generateObject.MockedMembers[0].ActualCallsField =
                new FieldDefinition(nameof(ActualCallsField), FieldAttributes.Public, new FunctionPointerType());

            Expression<Action> expr = () => TestMethod();
            if (shoudBeFailed)
                Assert.Throws<VerifiableException>(() => new TestMethod(generateObject).Execute(expr));
            else
                new TestMethod(generateObject).Execute(expr);
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
                SetupArguments = new object[] {1},
                ExpectedCallsCountFunc = i => i == expectedCalls,
                IsReturnObjectSet = true
            });

            var generateObject = _fakeGenerator.Generate(setupCollection, GetType().GetMethod(nameof(TestMethod)));
            generateObject.MockedMembers[0].ActualCallsField =
                new FieldDefinition(nameof(ActualCallsField), FieldAttributes.Public, new FunctionPointerType());

            Expression<Action> expr = () => TestMethod();
            if (shoudBeFailed)
                Assert.Throws<ExpectedCallsException>(() => new TestMethod(generateObject).Execute(expr));
            else
                new TestMethod(generateObject).Execute(expr);
        }
    }
}
