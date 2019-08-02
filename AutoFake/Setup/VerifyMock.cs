using System;
using System.Collections.Generic;
using AutoFake.Exceptions;
using AutoFake.Expression;
using Mono.Cecil.Cil;

namespace AutoFake.Setup
{
    internal class VerifyMock : Mock
    {
        public VerifyMock(IInvocationExpression invocationExpression) : base(invocationExpression)
        {
        }

        public override void Inject(IMethodMocker methodMocker, ILProcessor ilProcessor, Instruction instruction)
        {
            if (CheckSourceMemberCalls)
            {
                var arguments = methodMocker.SaveMethodCall(ilProcessor, instruction, CheckArguments);
                methodMocker.PushMethodArguments(ilProcessor, instruction, arguments);
            }
        }

        public override IList<object> Initialize(MockedMemberInfo mockedMemberInfo, Type type)
        {
            var parameters = base.Initialize(mockedMemberInfo, type);
            if (ExpectedCallsFunc != null)
            {
                var field = GetField(type, mockedMemberInfo.ExpectedCallsFuncField.Name);
                if (field == null)
                    throw new FakeGeneretingException(
                        $"'{mockedMemberInfo.ExpectedCallsFuncField.Name}' is not found in the generated object");
                field.SetValue(null, ExpectedCallsFunc);
            }
            return parameters;
        }

        public override void BeforeInjection(IMocker mocker)
        {
            if (CheckSourceMemberCalls)
            {
                mocker.GenerateSetupBodyField();
            }
            if (ExpectedCallsFunc != null)
                mocker.GenerateCallsCounterFuncField();
        }

        public override void AfterInjection(IMocker mocker, ILProcessor ilProcessor)
        {
            if (CheckSourceMemberCalls)
            {
                mocker.InjectVerification(ilProcessor, CheckArguments, ExpectedCallsFunc != null);
            }
        }
    }
}
