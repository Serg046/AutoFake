using System;
using System.Collections.Generic;
using AutoFake.Exceptions;
using AutoFake.Expression;
using Mono.Cecil.Cil;

namespace AutoFake.Setup
{
    internal class VerifyMock : Mock
    {
        private readonly Parameters _parameters;

        public VerifyMock(IInvocationExpression invocationExpression, Parameters parameters) : base(invocationExpression)
        {
            _parameters = parameters;
        }

        public override bool CheckArguments => _parameters.CheckArguments;

        public override Func<byte, bool> ExpectedCalls => _parameters.ExpectedCallsFunc;

        public override void Inject(IMethodMocker methodMocker, ILProcessor ilProcessor, Instruction instruction)
        {
            if (_parameters.CheckArguments || _parameters.ExpectedCallsFunc != null)
            {
                var arguments = methodMocker.SaveMethodCall(ilProcessor, instruction);
                methodMocker.PushMethodArguments(ilProcessor, instruction, arguments);
            }
        }

        public override IList<object> Initialize(MockedMemberInfo mockedMemberInfo, Type type)
        {
            var parameters = base.Initialize(mockedMemberInfo, type);
            if (_parameters.ExpectedCallsFunc != null)
            {
                var field = GetField(type, mockedMemberInfo.ExpectedCallsFuncField.Name);
                if (field == null)
                    throw new FakeGeneretingException(
                        $"'{mockedMemberInfo.ExpectedCallsFuncField.Name}' is not found in the generated object");
                field.SetValue(null, _parameters.ExpectedCallsFunc);
            }
            return parameters;
        }

        public override void PrepareForInjecting(IMocker mocker)
        {
            if (_parameters.CheckArguments || _parameters.ExpectedCallsFunc != null)
            {
                mocker.GenerateSetupBodyField();
            }
            if (_parameters.ExpectedCallsFunc != null)
                mocker.GenerateCallsCounterFuncField();
        }

        internal class Parameters
        {
            public bool CheckArguments { get; set; }
            public Func<byte, bool> ExpectedCallsFunc { get; set; }
        }
    }
}
