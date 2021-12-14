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
            IReadOnlyList<VariableDefinition> args,
            IEmitter emitter)
        {
            expression.Setup(e => e.GetArguments()).Returns(new List<IFakeArgument>
            {
                new FakeArgument(new EqualityArgumentChecker(1))
            }.ToReadOnlyList);
            var mock = new VerifyMock(processorFactory, expression.Object);
            proc.Setup(p => p.RecordMethodCall(It.IsAny<FieldDefinition>(), It.IsAny<FieldDefinition>(), It.IsAny<IReadOnlyList<Type>>()))
	            .Returns(args);
            mock.BeforeInjection(method);

            mock.Inject(emitter, Instruction.Create(OpCodes.Call, method));

            proc.Verify(m => m.RecordMethodCall(It.IsAny<FieldDefinition>(), It.IsAny<FieldDefinition>(), It.IsAny<IReadOnlyList<Type>>()), Times.Once());
            proc.Verify(m => m.PushMethodArguments(args), Times.Once());
        }

        [Theory, AutoMoqData]
        internal void Inject_ParametrizedSourceMember_ArgsPassed(
	        [Frozen] Mock<ISourceMember> sourceMember,
            [Frozen] Mock<IPrePostProcessor> preProc,
	        [Frozen] Mock<IProcessor> processor,
            MethodDefinition method,
            FieldDefinition body,
            Instruction instruction,
	        VerifyMock sut)
        {
	        var parameters = GetType()
		        .GetMethod(nameof(Inject_ParametrizedSourceMember_ArgsPassed),
			        BindingFlags.NonPublic | BindingFlags.Instance)
		        .GetParameters();
	        sourceMember.Setup(s => s.GetParameters()).Returns(parameters);
	        preProc.Setup(p => p.GenerateField(It.IsAny<string>(), typeof(IInvocationExpression))).Returns(body);

            sut.BeforeInjection(method);
            sut.Inject(Mock.Of<IEmitter>(), instruction);

	        processor.Verify(p => p.RecordMethodCall(
		        It.IsAny<FieldDefinition>(),
		        It.IsAny<FieldDefinition>(),
		        It.Is<IReadOnlyList<Type>>(prms => prms
			        .SequenceEqual(parameters.Select(prm => prm.ParameterType)))));
        }
    }
}
