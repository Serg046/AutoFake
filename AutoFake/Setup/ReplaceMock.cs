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
        public ReplaceMock(IInvocationExpression invocationExpression) : base(invocationExpression)
        {
        }

        public MethodDescriptor ReturnObject { get; set; }

        public override void Inject(IMethodMocker methodMocker, ILProcessor ilProcessor, Instruction instruction)
        {
            if (CheckSourceMemberCalls)
            {
                methodMocker.SaveMethodCall(ilProcessor, instruction, CheckArguments);
            }
            else
            {
                methodMocker.RemoveMethodArgumentsIfAny(ilProcessor, instruction);
            }

            ReplaceInstruction(methodMocker, ilProcessor, instruction);
        }

        private void ReplaceInstruction(IMethodMocker methodMocker, ILProcessor ilProcessor, Instruction instruction)
        {
            if (SourceMember.HasStackInstance)
                methodMocker.RemoveStackArgument(ilProcessor, instruction);

            if (ReturnObject != null)
                methodMocker.ReplaceToRetValueField(ilProcessor, instruction);
            else
                methodMocker.RemoveInstruction(ilProcessor, instruction);
        }

        public override IList<object> Initialize(MockedMemberInfo mockedMemberInfo, Type type)
        {
            var parameters = base.Initialize(mockedMemberInfo, type).ToList();
            if (ReturnObject != null)
            {
                var field = GetField(type, mockedMemberInfo.RetValueField.Name)
                    ?? throw new FakeGeneretingException($"'{mockedMemberInfo.RetValueField.Name}' is not found in the generated object");
                var obj = ReflectionUtils.Invoke(type.Assembly, ReturnObject);
                field.SetValue(null, obj);
                parameters.Add(obj);
            }

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
            if (ReturnObject != null)
                mocker.GenerateRetValueField(SourceMember.ReturnType);
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
