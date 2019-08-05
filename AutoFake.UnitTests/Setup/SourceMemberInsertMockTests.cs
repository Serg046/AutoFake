using AutoFake.Expression;
using AutoFake.Setup;
using Mono.Cecil.Cil;
using Moq;
using Xunit;

namespace AutoFake.UnitTests.Setup
{
    public class SourceMemberInsertMockTests
    {
        [Theory]
        [InlineData(InsertMock.Location.Top, true)]
        [InlineData(InsertMock.Location.Bottom, false)]
        internal void Inject_MethodDescriptor_Injected(InsertMock.Location location, bool injectBeforeCmd)
        {
            var descriptor = new MethodDescriptor("testType", "testMethodName");
            var mock = new SourceMemberInsertMock(Mock.Of<IInvocationExpression>(), descriptor, location);
            var mocker = new Mock<IMethodMocker>();
            var cmd = Instruction.Create(OpCodes.Nop);

            mock.Inject(mocker.Object, null, cmd);

            mocker.Verify(m => m.InjectCallback(null, cmd, descriptor, injectBeforeCmd));
        }
    }
}
