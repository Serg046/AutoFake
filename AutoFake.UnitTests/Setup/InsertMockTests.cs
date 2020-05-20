using AutoFake.Setup.Mocks;
using AutoFixture.Xunit2;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Moq;
using Xunit;

namespace AutoFake.UnitTests.Setup
{
    public class InsertMockTests
    {
        [Theory]
        [InlineData(InsertMock.Location.Top, 1, 2, true)]
        [InlineData(InsertMock.Location.Bottom, 1, 2, false)]
        internal void IsSourceInstruction_Instruction_Success(InsertMock.Location location, int first, int last, bool shouldBeFirst)
        {
            var method = new MethodDefinition(null, MethodAttributes.Assembly, new FunctionPointerType());
            var firstInstruction = Instruction.Create(OpCodes.Ldc_I4, first);
            var lastInstruction = Instruction.Create(OpCodes.Ldc_I4, last);
            method.Body.Instructions.Add(firstInstruction);
            method.Body.Instructions.Add(lastInstruction);

            var mock = new InsertMock(null, null, location);

            Assert.True(mock.IsSourceInstruction(method, shouldBeFirst ? firstInstruction : lastInstruction));
            Assert.False(mock.IsSourceInstruction(method, shouldBeFirst ? lastInstruction : firstInstruction));
        }

        [Fact]
        public void IsSourceInstruction_UnexpectedLocation_False()
        {
            var mock = new InsertMock(null, null, (InsertMock.Location)(-1));

            Assert.False(mock.IsSourceInstruction(null, null));
        }

        [Theory, AutoMoqData]
        internal void Inject_Instruction_Injected([Frozen]Mock<IProcessor> proc, InsertMock sut)
        {
            var cmd = Instruction.Create(OpCodes.Nop);

            sut.Inject(null, cmd);

            proc.Verify(m => m.InjectCallback(sut.Action, true));
        }

        [Fact]
        public void Initialize_GeneratedType_Nothing()
        {
            var mock = new InsertMock(null, null, InsertMock.Location.Top);

            var parameters = mock.Initialize(null);

            Assert.Empty(parameters);
        }
    }
}
