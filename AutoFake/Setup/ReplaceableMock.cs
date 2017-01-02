using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoFake.Exceptions;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup
{
    internal class ReplaceableMock : Mock
    {
        private readonly Parameters _parameters;

        public ReplaceableMock(MethodInfo method, List<FakeArgument> setupArguments, Parameters parameters)
            : base(method, setupArguments)
        {
            _parameters = parameters;
        }

        private bool NeedCallsCounter =>  _parameters.NeedCheckArguments || _parameters.ExpectedCallsCountFunc != null;

        public override void Inject(IMethodMocker methodMocker, ILProcessor ilProcessor, Instruction instruction)
        {
            if (NeedCallsCounter)
            {
                methodMocker.InjectCurrentPositionSaving(ilProcessor, instruction);
                methodMocker.MemberInfo.SourceCodeCallsCount++;
            }
            if (_parameters.Callback != null)
                methodMocker.InjectCallback(ilProcessor, instruction);

            ProcessArguments(methodMocker, ilProcessor, instruction);
            ReplaceInstruction(methodMocker, ilProcessor, instruction);
        }

        private void ReplaceInstruction(IMethodMocker methodMocker, ILProcessor ilProcessor, Instruction instruction)
        {
            var methodReference = instruction.Operand as MethodReference;
            if (methodReference == null)
                throw new FakeGeneretingException("Operand of the mocked instruction must be a method");
            if (!methodReference.Resolve().IsStatic)
                methodMocker.RemoveStackArgument(ilProcessor, instruction);

            if (_parameters.IsReturnObjectSet)
                methodMocker.ReplaceToRetValueField(ilProcessor, instruction);
            else
                methodMocker.RemoveInstruction(ilProcessor, instruction);
        }

        private void ProcessArguments(IMethodMocker methodMocker, ILProcessor ilProcessor, Instruction instruction)
        {
            if (SetupArguments.Any())
            {
                if (_parameters.NeedCheckArguments)
                {
                    var arguments = methodMocker.PopMethodArguments(ilProcessor, instruction); 
                    methodMocker.MemberInfo.AddArguments(arguments);
                }
                else
                {
                    methodMocker.RemoveMethodArguments(ilProcessor, instruction);
                }
            }
        }

        public override void Verify(MockedMemberInfo mockedMemberInfo, GeneratedObject generatedObject)
            => Verify(mockedMemberInfo, generatedObject, _parameters.NeedCheckArguments, _parameters.ExpectedCallsCountFunc);

        public override void Initialize(MockedMemberInfo mockedMemberInfo, GeneratedObject generatedObject)
        {
            if (_parameters.IsReturnObjectSet)
            {
                var field = GetField(generatedObject, mockedMemberInfo.RetValueField.Name);
                if (field == null)
                    throw new FakeGeneretingException(
                        $"'{mockedMemberInfo.RetValueField.Name}' is not found in the generated object");
                field.SetValue(null, _parameters.ReturnObject);
            }

            if (_parameters.Callback != null)
            {
                var field = GetField(generatedObject, mockedMemberInfo.CallbackField.Name);
                if (field == null)
                    throw new FakeGeneretingException(
                        $"'{mockedMemberInfo.CallbackField.Name}' is not found in the generated object");
                field.SetValue(null, _parameters.Callback);
            }
        }

        public override void PrepareForInjecting(IMocker mocker)
        {
            if (NeedCallsCounter)
                mocker.GenerateCallsCounter();
            if (_parameters.IsReturnObjectSet)
                mocker.GenerateRetValueField();
            if (_parameters.Callback != null)
                mocker.GenerateCallbackField();
        }
        
        internal class Parameters
        {
            private object _returnObject;

            public bool NeedCheckArguments { get; set; }
            public Func<int, bool> ExpectedCallsCountFunc { get; set; }
            public bool IsReturnObjectSet { get; private set; }

            public object ReturnObject
            {
                get { return _returnObject; }
                set
                {
                    _returnObject = value;
                    IsReturnObjectSet = true;
                }
            }

            public Action Callback { get; set; }
        }
    }
}
