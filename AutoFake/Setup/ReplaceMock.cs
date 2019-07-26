using System;
using System.Collections.Generic;
using System.Linq;
using AutoFake.Exceptions;
using AutoFake.Expression;
using Mono.Cecil.Cil;

namespace AutoFake.Setup
{
    internal class ReplaceMock : Mock
    {
        private readonly Parameters _parameters;

        public ReplaceMock(IInvocationExpression invocationExpression, Parameters parameters) : base(invocationExpression)
        {
            _parameters = parameters;
        }

        public override Func<byte, bool> ExpectedCalls => _parameters.ExpectedCallsFunc;

        public override bool CheckArguments => _parameters.CheckArguments;

        public override void Inject(IMethodMocker methodMocker, ILProcessor ilProcessor, Instruction instruction)
        {
            if (_parameters.CheckArguments || _parameters.ExpectedCallsFunc != null)
            {
                methodMocker.SaveMethodCall(ilProcessor, instruction);
            }
            else
            {
                methodMocker.RemoveMethodArgumentsIfAny(ilProcessor, instruction);
            }

            if (_parameters.Callback != null)
                methodMocker.InjectCallback(ilProcessor, instruction);

            ReplaceInstruction(methodMocker, ilProcessor, instruction);
        }

        private void ReplaceInstruction(IMethodMocker methodMocker, ILProcessor ilProcessor, Instruction instruction)
        {
            if (SourceMember.HasStackInstance)
                methodMocker.RemoveStackArgument(ilProcessor, instruction);

            if (_parameters.ReturnObject != null)
                methodMocker.ReplaceToRetValueField(ilProcessor, instruction);
            else
                methodMocker.RemoveInstruction(ilProcessor, instruction);
        }

        public override IList<object> Initialize(MockedMemberInfo mockedMemberInfo, Type type)
        {
            var parameters = base.Initialize(mockedMemberInfo, type).ToList();
            if (_parameters.ReturnObject != null)
            {
                var field = GetField(type, mockedMemberInfo.RetValueField.Name)
                    ?? throw new FakeGeneretingException($"'{mockedMemberInfo.RetValueField.Name}' is not found in the generated object");
                var obj = ReflectionUtils.Invoke(type.Assembly, _parameters.ReturnObject);
                field.SetValue(null, obj);
                parameters.Add(obj);
            }

            if (_parameters.Callback != null)
            {
                var field = GetField(type, mockedMemberInfo.CallbackField.Name);
                if (field == null)
                    throw new FakeGeneretingException(
                        $"'{mockedMemberInfo.CallbackField.Name}' is not found in the generated object");
                field.SetValue(null, _parameters.Callback);
            }

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
            if (_parameters.ReturnObject != null)
                mocker.GenerateRetValueField();
            if (_parameters.Callback != null)
                mocker.GenerateCallbackField();
        }
        
        internal class Parameters
        {
            public bool CheckArguments { get; set; }
            public Func<byte, bool> ExpectedCallsFunc { get; set; }
            public MethodDescriptor ReturnObject { get; set; }
            public Action Callback { get; set; }
        }
    }
}
