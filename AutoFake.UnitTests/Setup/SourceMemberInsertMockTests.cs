using AutoFake.Expression;
using AutoFake.Setup.Mocks;
using AutoFixture.Xunit2;
using Mono.Cecil.Cil;
using Moq;
using Xunit;

namespace AutoFake.UnitTests.Setup
{
    public class SourceMemberInsertMockTests
    {
        [Theory]
        [InlineAutoMoqData(InsertMock.Location.Top, true)]
        [InlineAutoMoqData(InsertMock.Location.Bottom, false)]
        internal void Inject_MethodDescriptor_Injected(
            InsertMock.Location location,
            bool injectBeforeCmd,
            MethodDescriptor descriptor,
            [Frozen]Mock<IProcessor> proc,
            [Frozen]IProcessorFactory factory)
        {
            var mock = new SourceMemberInsertMock(factory, Mock.Of<IInvocationExpression>(), descriptor, location);
            var cmd = Instruction.Create(OpCodes.Nop);

            mock.Inject(null, cmd);

            proc.Verify(m => m.InjectCallback(descriptor, injectBeforeCmd));
        }
    }
}
