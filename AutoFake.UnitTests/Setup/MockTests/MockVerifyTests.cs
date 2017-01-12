using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoFake.Exceptions;
using AutoFake.Setup;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;
using FieldAttributes = Mono.Cecil.FieldAttributes;

namespace AutoFake.UnitTests.Setup.MockTests
{
    public class MockVerifyTests
    {
        private const int INSTALLED_METHOD_ARGUMENT = 78;

        private readonly ReplaceableMock.Parameters _parameters;
        private readonly GeneratedObject _generatedObject;
        private readonly Mock _mock;

        public MockVerifyTests()
        {
            _parameters = new ReplaceableMock.Parameters();
            _parameters.NeedCheckArguments = true;
            _parameters.ExpectedCallsCountFunc = i => i == 1;
            _mock = GetMock();

            var typeInfo = new TypeInfo(typeof(TestClass), new List<FakeDependency>());
            _generatedObject = new GeneratedObject(typeInfo);
            _generatedObject.Build();
        }

        private FieldDefinition CreateFieldDefinition(string fieldName) => new FieldDefinition(fieldName, FieldAttributes.Static, new FunctionPointerType());

        private Mock GetMock() => GetMock(CreateArgument(INSTALLED_METHOD_ARGUMENT));
        private Mock GetMock(params FakeArgument[] arguments)
            => new MockFake(typeof(TestClass).GetMethod(nameof(TestClass.MockedMethod)),
                arguments.ToList(),
                _parameters);

        private FakeArgument CreateArgument(int arg) => new FakeArgument(new EqualityArgumentChecker(arg));

        private MockedMemberInfo GetMockedMemberInfo()
            => GetMockedMemberInfo(new List<List<FieldDefinition>>
            {
                new List<FieldDefinition> {CreateFieldDefinition(nameof(TestClass.ArgumentField))}
            });

        private MockedMemberInfo GetMockedMemberInfo(List<List<FieldDefinition>> argFields)
            => GetMockedMemberInfo(_mock, argFields);

        private MockedMemberInfo GetMockedMemberInfo(AutoFake.Setup.Mock mock, List<List<FieldDefinition>> argFields)
        {
            var mockedMemberInfo = new MockedMemberInfo(mock, null, "suffix");
            mockedMemberInfo.ActualCallsField = CreateFieldDefinition(nameof(TestClass.ActualCallsCountField));
            argFields.ForEach(mockedMemberInfo.AddArguments);
            return mockedMemberInfo;
        }

        [Fact]
        public void Verify_ValidData_Success()
        {
            var mockedMemberInfo = GetMockedMemberInfo();

            _mock.Verify(mockedMemberInfo, _generatedObject);
        }

        [Fact]
        public void Verify_NoArguments_Fails()
        {
            var mockedMemberInfo = GetMockedMemberInfo(new List<List<FieldDefinition>>());

            Assert.Throws<ArgumentOutOfRangeException>(() => _mock.Verify(mockedMemberInfo, _generatedObject));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        public void Verify_IncorrectArgumentsCount_Fails(int argumentsCount)
        {
            var args = new List<FieldDefinition>();
            for (int i = 0; i < argumentsCount; i++)
            {
                args.Add(CreateFieldDefinition(nameof(TestClass.ArgumentField)));
            }
            var list = new List<List<FieldDefinition>>();
            list.Add(args);
            var mockedMemberInfo = GetMockedMemberInfo(list);

            Assert.Throws<FakeGeneretingException>(() => _mock.Verify(mockedMemberInfo, _generatedObject));
        }

        [Fact]
        public void Verify_NoArgumentField_Fails()
        {
            var mockedMemberInfo = GetMockedMemberInfo(new List<List<FieldDefinition>>
            {
                new List<FieldDefinition> {CreateFieldDefinition("IncorrectFieldName")}
            });

            Assert.Throws<FakeGeneretingException>(() => _mock.Verify(mockedMemberInfo, _generatedObject));
        }

        [Theory]
        [InlineData(INSTALLED_METHOD_ARGUMENT + 1, true)]
        [InlineData(INSTALLED_METHOD_ARGUMENT, false)]
        public void Verify_IncorrectArgumentValue_Fails(int value, bool shouldBeFailed)
        {
            var mock = GetMock(CreateArgument(value));
            var mockedMemberInfo = GetMockedMemberInfo(mock, new List<List<FieldDefinition>>
            {
                new List<FieldDefinition> {CreateFieldDefinition(nameof(TestClass.ArgumentField))}
            });

            if (shouldBeFailed)
                Assert.Throws<VerifiableException>(() => mock.Verify(mockedMemberInfo, _generatedObject));
            else
                mock.Verify(mockedMemberInfo, _generatedObject);
        }

        [Fact]
        public void Verify_NotNeedCheckArguments_CallsCountVerified()
        {
            _parameters.NeedCheckArguments = false;
            var mockedMemberInfo = GetMockedMemberInfo();

            _mock.Verify(mockedMemberInfo, _generatedObject);
        }

        private class TestClass
        {
            internal static List<int> ActualCallsCountField = new List<int> { 0 };
            internal static int ArgumentField = INSTALLED_METHOD_ARGUMENT;

            public void TestMethod(int value)
            {
                MockedMethod(value);
            }

            public void MockedMethod(int value)
            {
            }
        }

        private class MockFake : Mock
        {
            private readonly ReplaceableMock.Parameters _parameters;

            public MockFake(MethodInfo method, List<FakeArgument> setupArguments, ReplaceableMock.Parameters parameters)
                : base(new SourceMethod(method), setupArguments)
            {
                _parameters = parameters;
            }

            public override void PrepareForInjecting(IMocker mocker)
            {
                throw new System.NotImplementedException();
            }

            public override void Inject(IMethodMocker methodMocker, ILProcessor ilProcessor, Instruction instruction)
            {
                throw new System.NotImplementedException();
            }

            public override void Initialize(MockedMemberInfo mockedMemberInfo, GeneratedObject generatedObject)
            {
                throw new System.NotImplementedException();
            }

            public override void Verify(MockedMemberInfo mockedMemberInfo, GeneratedObject generatedObject)
                => Verify(mockedMemberInfo, generatedObject, _parameters.NeedCheckArguments, _parameters.ExpectedCallsCountFunc);
        }
    }
}
