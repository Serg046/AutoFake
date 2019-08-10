using System;
using System.Reflection;
using AutoFake.Exceptions;
using AutoFake.Expression;
using AutoFake.Setup;
using AutoFake.Setup.Mocks;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Moq;
using Xunit;
using FieldAttributes = Mono.Cecil.FieldAttributes;

namespace AutoFake.UnitTests.Setup
{
    public class SourceMemberMockTests
    {
        //[Fact]
        //public void Initialize_SetupBodyField_ExpressionSet()
        //{
        //    var mock = new MockFake(GetMethod(nameof(TestClass.TestMethod)));
        //    var mockedMember = new MockedMemberInfo(mock, null);
        //    mockedMember.SetupBodyField = new FieldDefinition(nameof(TestClass.InvocationExpression),
        //        Mono.Cecil.FieldAttributes.Assembly, new FunctionPointerType());

        //    Assert.Null(TestClass.InvocationExpression);
        //    mock.Initialize(mockedMember, typeof(TestClass));

        //    Assert.Equal(mock.InvocationExpression, TestClass.InvocationExpression);
        //    TestClass.InvocationExpression = null;
        //}

        //[Fact]
        //public void Initialize_IncorrectSetupBodyField_Fails()
        //{
        //    var mock = new MockFake(GetMethod(nameof(TestClass.TestMethod)));
        //    var mockedMember = new MockedMemberInfo(mock, null);
        //    mockedMember.SetupBodyField = new FieldDefinition(nameof(TestClass.InvocationExpression) + "salt",
        //        Mono.Cecil.FieldAttributes.Assembly, new FunctionPointerType());

        //    Assert.Throws<FakeGeneretingException>(() => mock.Initialize(mockedMember, typeof(TestClass)));
        //}

        //[Fact]
        //public void Initialize_NoSetupBodyField_NoEffect()
        //{
        //    var mock = new MockFake(GetMethod(nameof(TestClass.TestMethod)));
        //    var mockedMemberInfo = new MockedMemberInfo(mock, null);

        //    mock.Initialize(mockedMemberInfo, typeof(TestClass));

        //    Assert.Null(TestClass.InvocationExpression);
        //}

        //[Fact]
        //public void Initialize_ExpectedCallsFunc_Set()
        //{
        //    var type = typeof(TestClass);
        //    var mock = new MockFake(GetMethod(nameof(TestClass.TestMethod)));
        //    mock.ExpectedCallsFunc = i => true;
        //    var mockedMemberInfo = new MockedMemberInfo(mock, null);
        //    mockedMemberInfo.ExpectedCallsFuncField = new FieldDefinition(nameof(TestClass.ExpectedCallsFuncField),
        //        FieldAttributes.Assembly, new FunctionPointerType());

        //    Assert.Null(TestClass.ExpectedCallsFuncField);
        //    mock.Initialize(mockedMemberInfo, type);

        //    Assert.Equal(mock.ExpectedCallsFunc, TestClass.ExpectedCallsFuncField);
        //    TestClass.ExpectedCallsFuncField = null;
        //}

        //[Fact]
        //public void Initialize_IncorrectExpectedCallsField_Fails()
        //{
        //    var mock = new MockFake(GetMethod(nameof(TestClass.TestMethod)));
        //    mock.ExpectedCallsFunc = i => true;
        //    var mockedMemberInfo = new MockedMemberInfo(mock, null);
        //    mockedMemberInfo.ExpectedCallsFuncField = new FieldDefinition(nameof(TestClass.ExpectedCallsFuncField) + "salt",
        //        FieldAttributes.Assembly, new FunctionPointerType());

        //    Assert.Throws<FakeGeneretingException>(() => mock.Initialize(mockedMemberInfo, typeof(TestClass)));
        //}

        //[Fact]
        //public void Initialize_NoExpectedCallsField_NoEffect()
        //{
        //    var mock = new MockFake(GetMethod(nameof(TestClass.TestMethod)));
        //    var mockedMemberInfo = new MockedMemberInfo(mock, null);

        //    mock.Initialize(mockedMemberInfo, typeof(TestClass));

        //    Assert.Null(TestClass.ExpectedCallsFuncField);
        //}

        //[Theory]
        //[InlineData(false, false, false)]
        //[InlineData(false, true, true)]
        //[InlineData(true, false, true)]
        //[InlineData(true, true, true)]
        //public void BeforeInjection_NeedCheckArgumentsOrExpectedCallsCount_GenerateSetupBodyFieldInjected(
        //    bool needCheckArguments, bool expectedCallsCount, bool shouldBeInjected)
        //{
        //    var mock = new MockFake(GetMethod(nameof(TestClass.TestMethod)));
        //    mock.CheckArguments = needCheckArguments;
        //    mock.ExpectedCallsFunc = expectedCallsCount ? i => i == 1 : (Func<byte, bool>)null;
        //    var mocker = new Mock<IMocker>();

        //    mock.BeforeInjection(mocker.Object);

        //    mocker.Verify(m => m.GenerateSetupBodyField(), shouldBeInjected ? Times.AtLeastOnce() : Times.Never());
        //}

        //[Theory]
        //[InlineData(false, false)]
        //[InlineData(true, true)]
        //public void BeforeInjection_ExpectedCallsFunc_Injected(bool callsCounter, bool shouldBeInjected)
        //{
        //    var mock = new MockFake(GetMethod(nameof(TestClass.TestMethod)));
        //    if (callsCounter) mock.ExpectedCallsFunc = i => true;
        //    var mocker = new Mock<IMocker>();

        //    mock.BeforeInjection(mocker.Object);

        //    mocker.Verify(m => m.GenerateCallsCounterFuncField(), shouldBeInjected ? Times.AtLeastOnce() : Times.Never());
        //}

        //[Fact]
        //public void IsSourceInstruction_Cmd_CallsSourceMember()
        //{
        //    var sourceMember = new Mock<ISourceMember>();
        //    var mock = new MockFake(sourceMember.Object);
        //    var cmd = Instruction.Create(OpCodes.Nop);

        //    mock.IsSourceInstruction(null, null, cmd);

        //    sourceMember.Verify(s => s.IsSourceInstruction(null, cmd));
        //}

        //[Theory]
        //[InlineData(false, false, false)]
        //[InlineData(true, false, true)]
        //[InlineData(false, true, true)]
        //[InlineData(true, true, true)]
        //public void AfterInjection_Flags_VerificationInjected(bool checkArgs, bool expectedCalls, bool injected)
        //{
        //    var mock = new MockFake(GetMethod(nameof(TestClass.TestMethod)));
        //    mock.CheckArguments = checkArgs;
        //    mock.ExpectedCallsFunc = expectedCalls ? i => true : (Func<byte, bool>)null;
        //    var mocker = new Mock<IMocker>();

        //    mock.AfterInjection(mocker.Object, null);

        //    mocker.Verify(m => m.InjectVerification(null, checkArgs, expectedCalls), injected ? Times.Once() : Times.Never());
        //}

        //private MethodInfo GetMethod(string methodName, params Type[] arguments) => GetMethod<TestClass>(methodName, arguments);
        //private MethodInfo GetMethod<T>(string methodName, params Type[] arguments) => typeof(T).GetMethod(methodName, arguments);

        //private class MockFake : SourceMemberMock
        //{
        //    public MockFake(MethodInfo method) : this(new SourceMethod(method))
        //    {
        //    }

        //    public MockFake(ISourceMember sourceMember) : this(Moq.Mock.Of<IInvocationExpression>(
        //        m => m.GetSourceMember() == sourceMember))
        //    {
        //    }

        //    private MockFake(IInvocationExpression invocationExpression) : base(invocationExpression)
        //    {
        //        InvocationExpression = invocationExpression;
        //    }

        //    public IInvocationExpression InvocationExpression { get; }

        //    public override void Inject(IMethodMocker methodMocker, ILProcessor ilProcessor, Instruction instruction)
        //        => throw new NotImplementedException();
        //}

        //private class TestClass
        //{
        //    internal static Func<byte, bool> ExpectedCallsFuncField;
        //    internal static IInvocationExpression InvocationExpression;

        //    public void TestMethod()
        //    {
        //    }

        //    public int TestMethod(int arg) => 5;
        //}
    }
}
