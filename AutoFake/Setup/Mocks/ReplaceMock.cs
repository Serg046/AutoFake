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
                var returnInstance = ReturnObject.Instance
                                 ?? ReflectionUtils.Invoke(type.Assembly, ReturnObject.Descriptor);
                var field = GetField(type, _retValueField.Name)
                    ?? throw new InitializationException($"'{_retValueField.Name}' is not found in the generated object");
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
                _retValueField = PrePostProcessor.GenerateField(GetFieldName(method.Name, "RetValue"), SourceMember.ReturnType);
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
