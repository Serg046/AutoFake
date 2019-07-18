﻿using System;
using System.Collections.Generic;
using System.Reflection;
using AutoFake.Exceptions;
using AutoFake.Expression;
using Mono.Cecil.Cil;

namespace AutoFake.Setup
{
    internal class VerifiableMock : Mock
    {
        private readonly Parameters _parameters;

        public VerifiableMock(IInvocationExpression invocationExpression, Parameters parameters) : base(invocationExpression)
        {
            _parameters = parameters;
        }

        public override bool CheckArguments => _parameters.NeedCheckArguments;

        public override Func<byte, bool> ExpectedCalls => _parameters.ExpectedCallsCountFunc;

        private bool NeedCallsCounter => _parameters.NeedCheckArguments || _parameters.ExpectedCallsCountFunc != null;

        public override void Inject(IMethodMocker methodMocker, ILProcessor ilProcessor, Instruction instruction)
        {
            if (_parameters.NeedCheckArguments || _parameters.ExpectedCallsCountFunc != null)
            {
                var arguments = methodMocker.SaveMethodCall(ilProcessor, instruction);
                methodMocker.PushMethodArguments(ilProcessor, instruction, arguments);
            }
        }

        public override IList<object> Initialize(MockedMemberInfo mockedMemberInfo, Type type)
        {
            var parameters = base.Initialize(mockedMemberInfo, type);
            if (_parameters.ExpectedCallsCountFunc != null)
            {
                var field = GetField(type, mockedMemberInfo.ExpectedCallsFuncField.Name);
                if (field == null)
                    throw new FakeGeneretingException(
                        $"'{mockedMemberInfo.ExpectedCallsFuncField.Name}' is not found in the generated object");
                field.SetValue(null, _parameters.ExpectedCallsCountFunc);
            }
            return parameters;
        }

        public override void PrepareForInjecting(IMocker mocker)
        {
            if (_parameters.NeedCheckArguments || _parameters.ExpectedCallsCountFunc != null)
            {
                mocker.GenerateSetupBodyField();
            }
            if (_parameters.ExpectedCallsCountFunc != null)
                mocker.GenerateCallsCounterFuncField();
        }

        internal class Parameters
        {
            public bool NeedCheckArguments { get; set; }
            public Func<byte, bool> ExpectedCallsCountFunc { get; set; }
        }
    }
}
