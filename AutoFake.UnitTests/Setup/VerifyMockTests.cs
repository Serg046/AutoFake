using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoFake.Expression;
using AutoFake.Setup;
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
            IProcessorFactory processorFactory,
            Mock<IInvocationExpression> expression,
            MethodDefinition method,
            FieldDefinition accumulator,
            IEmitter emitter)
        {
            expression.Setup(e => e.GetArguments()).Returns(new List<IFakeArgument>
            {
                new FakeArgument(needCheckArguments 
                    ? new EqualityArgumentChecker(1) 
                    : (IFakeArgumentChecker)new SuccessfulArgumentChecker())
            });
            var mock = new VerifyMock(processorFactory, expression.Object);
            mock.ExpectedCalls = expectedCallsCountFunc ? new Func<byte, bool>(i => true) : null;
            var runtimeArgs = new List<VariableDefinition>();
            preProc.Setup(p => p.GenerateCallsAccumulator(It.IsAny<string>(), It.IsAny<MethodBody>())).Returns(accumulator);
            proc.Setup(p => p.SaveMethodCall(It.IsAny<FieldDefinition>(), needCheckArguments, It.IsAny<IEnumerable<Type>>())).Returns(runtimeArgs);
            mock.BeforeInjection(method);

            mock.Inject(emitter, Instruction.Create(OpCodes.Call, method));

            if (mustBeInjected)
            {
                proc.Verify(m => m.SaveMethodCall(accumulator, needCheckArguments, It.IsAny<IEnumerable<Type>>()), Times.Once());
                proc.Verify(m => m.PushMethodArguments(runtimeArgs), Times.Once());
            }
            else
            {
                proc.Verify(m => m.SaveMethodCall(It.IsAny<FieldDefinition>(), needCheckArguments, It.IsAny<IEnumerable<Type>>()), Times.Never);
                proc.Verify(m => m.PushMethodArguments(It.IsAny<IEnumerable<VariableDefinition>>()), Times.Never);
            }
        }

        [Theory, AutoMoqData]
        internal void Inject_ParametrizedSourceMember_ArgsPassed(
	        [Frozen] Mock<ISourceMember> sourceMember,
	        [Frozen] Mock<IProcessor> processor,
            Instruction instruction,
	        VerifyMock sut)
        {
	        var parameters = GetType()
		        .GetMethod(nameof(Inject_ParametrizedSourceMember_ArgsPassed),
			        BindingFlags.NonPublic | BindingFlags.Instance)
		        .GetParameters();
	        sourceMember.Setup(s => s.GetParameters()).Returns(parameters);

	        sut.Inject(Mock.Of<IEmitter>(), instruction);

	        processor.Verify(p => p.SaveMethodCall(
		        It.IsAny<FieldDefinition>(),
		        It.IsAny<bool>(),
		        It.Is<IEnumerable<Type>>(prms => prms
			        .SequenceEqual(parameters.Select(prm => prm.ParameterType)))));
        }
    }
}
