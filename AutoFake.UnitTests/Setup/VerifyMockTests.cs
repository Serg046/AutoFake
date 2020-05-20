using System.Collections.Generic;
using AutoFake.Setup.Mocks;
using AutoFixture.Xunit2;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Moq;
using Xunit;
using MethodBody = Mono.Cecil.Cil.MethodBody;

namespace AutoFake.UnitTests.Setup
{
    public class VerifyMockTests
    {
        [Theory]
        [InlineAutoMoqData(false, false, false)]
        [InlineAutoMoqData(false, true, true)]
        [InlineAutoMoqData(true, false, true)]
        [InlineAutoMoqData(true, true, true)]
        internal void InjectNeedCheckArgumentsOrExpectedCallsCount_ArgumentsSaved(
            bool needCheckArguments, bool expectedCallsCountFunc, bool mustBeInjected,
            [Frozen]Mock<IPrePostProcessor> preProc,
            [Frozen]Mock<IProcessor> proc,
            MethodDefinition method,
            VariableDefinition accumulator,
            IEmitter emitter,
            VerifyMock mock)
        {
            if (!expectedCallsCountFunc) mock.ExpectedCalls = null;
            mock.CheckArguments = needCheckArguments;
            var runtimeArgs = new List<VariableDefinition>();
            preProc.Setup(p => p.GenerateCallsAccumulator(It.IsAny<MethodBody>())).Returns(accumulator);
            proc.Setup(p => p.SaveMethodCall(It.IsAny<VariableDefinition>(), needCheckArguments)).Returns(runtimeArgs);
            mock.BeforeInjection(method);

            mock.Inject(emitter, Instruction.Create(OpCodes.Call, method));

            if (mustBeInjected)
            {
                proc.Verify(m => m.SaveMethodCall(accumulator, needCheckArguments), Times.Once());
                proc.Verify(m => m.PushMethodArguments(runtimeArgs), Times.Once());
            }
            else
            {
                proc.Verify(m => m.SaveMethodCall(It.IsAny<VariableDefinition>(), needCheckArguments), Times.Never);
                proc.Verify(m => m.PushMethodArguments(It.IsAny<IEnumerable<VariableDefinition>>()), Times.Never);
            }
        }
    }
}
