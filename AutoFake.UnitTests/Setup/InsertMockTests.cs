using System;
using AutoFake.Setup;
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
            var method = new MethodBody(null);
            var firstInstruction = Instruction.Create(OpCodes.Ldc_I4, first);
            var lastInstruction = Instruction.Create(OpCodes.Ldc_I4, last);
            method.Instructions.Add(firstInstruction);
            method.Instructions.Add(lastInstruction);

            var mock = new InsertMock(() => { }, location);

            Assert.True(mock.IsSourceInstruction(null, method, shouldBeFirst ? firstInstruction : lastInstruction));
            Assert.False(mock.IsSourceInstruction(null, method, shouldBeFirst ? lastInstruction : firstInstruction));
        }

        [Fact]
        public void Inject_Instruction_Injected()
        {
            var mocker = new Mock<IMethodMocker>();
            var mock = new InsertMock(() => { }, InsertMock.Location.Top);
            var cmd = Instruction.Create(OpCodes.Nop);

            mock.Inject(mocker.Object, null, cmd);

            mocker.Verify(m => m.InjectCallback(null, cmd, It.IsAny<MethodDescriptor>()));
        }

        [Fact]
        public void Initialize_GeneratedType_Nothing()
        {
            var mock = new InsertMock(() => { }, InsertMock.Location.Top);

            var parameters = mock.Initialize(null, null);

            Assert.Empty(parameters);
        }
    }
}
