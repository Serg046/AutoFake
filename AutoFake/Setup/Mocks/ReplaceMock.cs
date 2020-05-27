using System;
using System.Collections.Generic;
using System.Linq;
using AutoFake.Exceptions;
using AutoFake.Expression;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup.Mocks
{
    internal class ReplaceMock : SourceMemberMock
    {
        private FieldDefinition _retValueField;
        private TypeReference _typeReference;

        public ReplaceMock(IProcessorFactory processorFactory, IInvocationExpression invocationExpression)
            : base(processorFactory, invocationExpression)
        {
        }

        private Return _returnObject;
        public Return ReturnObject
        {
            get => _returnObject;
            set
            {
                _returnObject = value;
                if (_returnObject?.Instance != null)
                {
                    _typeReference = PrePostProcessor.GetTypeReference(_returnObject.Instance.GetType());
                }
            }
        }

        public override void ProcessInstruction(Instruction instruction)
        {
            if (_typeReference != null)
            {
                if (instruction.Operand is FieldReference field && field.FieldType.FullName == _typeReference.FullName)
                {
                    field.FieldType = _typeReference;
                }
                else if (instruction.Operand is MethodReference method)
                {
                    if (method.ReturnType.FullName == _typeReference.FullName)
                    {
                        method.ReturnType = _typeReference;
                    }
                    for (var i = 0; i < method.Parameters.Count; i++)
                    {
                        if (method.Parameters[i].ParameterType.FullName == _typeReference.FullName)
                        {
                            method.Parameters[i].ParameterType = _typeReference;
                        }
                    }
                }
            }
        }

        public override void Inject(IEmitter emitter, Instruction instruction)
        {
            var processor = ProcessorFactory.CreateProcessor(emitter, instruction);
            if (CheckSourceMemberCalls)
            {
                processor.SaveMethodCall(CallsAccumulator, CheckArguments);
            }
            else
            {
                processor.RemoveMethodArgumentsIfAny();
            }

            ReplaceInstruction(processor, instruction);
        }

        private void ReplaceInstruction(IProcessor processor, Instruction instruction)
        {
            if (SourceMember.HasStackInstance) processor.RemoveStackArgument();

            if (ReturnObject != null)
                processor.ReplaceToRetValueField(_retValueField);
            else
                processor.RemoveInstruction(instruction);
        }

        public override IList<object> Initialize(Type type)
        {
            var parameters = base.Initialize(type).ToList();
            if (ReturnObject != null)
            {
                object returnInstance;
                returnInstance = ReturnObject.Instance
                                 ?? ReflectionUtils.Invoke(type.Assembly, ReturnObject.Descriptor);
                var field = GetField(type, _retValueField.Name)
                    ?? throw new FakeGeneretingException($"'{_retValueField.Name}' is not found in the generated object");
                field.SetValue(null, returnInstance);
                parameters.Add(returnInstance);
            }
            return parameters;
        }

        public override void BeforeInjection(MethodDefinition method)
        {
            base.BeforeInjection(method);
            if (ReturnObject != null)
            {
                _retValueField = PrePostProcessor.GenerateRetValueField(
                    GetFieldName(method.Name, "RetValue"), SourceMember.ReturnType);
                if (_typeReference != null)
                {
                    _retValueField.FieldType = _typeReference;
                }
            }
        }

        internal class Return
        {
            public Return(MethodDescriptor descriptor)
            {
                Descriptor = descriptor;
            }

            public Return(object instance)
            {
                Instance = instance;
            }

            public MethodDescriptor Descriptor { get; }
            public object Instance { get; }
        }
    }
}
