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

namespace AutoFake.UnitTests.Setup
{
    public class VerifyMockTests
    {
        [Theory, AutoMoqData]
        internal void InjectNeedCheckArgumentsOrExpectedCallsCount_ArgumentsSaved(
            [Frozen]Mock<IProcessor> proc,
            IProcessorFactory processorFactory,
            Mock<IInvocationExpression> expression,
            MethodDefinition method,
            List<VariableDefinition> args,
            IEmitter emitter)
        {
            expression.Setup(e => e.GetArguments()).Returns(new List<IFakeArgument>
            {
                new FakeArgument(new EqualityArgumentChecker(1))
            });
            var mock = new VerifyMock(processorFactory, expression.Object);
            proc.Setup(p => p.SaveMethodCall(It.IsAny<FieldDefinition>(), It.IsAny<FieldDefinition>(), It.IsAny<IEnumerable<Type>>()))
	            .Returns(args);
            mock.BeforeInjection(method);

            mock.Inject(emitter, Instruction.Create(OpCodes.Call, method));

            proc.Verify(m => m.SaveMethodCall(It.IsAny<FieldDefinition>(), It.IsAny<FieldDefinition>(), It.IsAny<IEnumerable<Type>>()), Times.Once());
            proc.Verify(m => m.PushMethodArguments(args), Times.Once());
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
		        It.IsAny<FieldDefinition>(),
		        It.Is<IEnumerable<Type>>(prms => prms
			        .SequenceEqual(parameters.Select(prm => prm.ParameterType)))));
        }
    }
}
