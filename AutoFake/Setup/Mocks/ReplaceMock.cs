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

        public Type ReturnType { get; set; }
        public object ReturnObject { get; set; }

        public override void Inject(IEmitter emitter, Instruction instruction)
        {
            var processor = ProcessorFactory.CreateProcessor(emitter, instruction);
            if (CheckSourceMemberCalls)
            {
                processor.SaveMethodCall(CallsAccumulator, CheckArguments,
	                SourceMember.GetParameters().Select(p => p.ParameterType));
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
                var field = GetField(type, _retValueField.Name)
                            ?? throw new InitializationException($"'{_retValueField.Name}' is not found in the generated object");
                field.SetValue(null, ReturnObject);
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
    }
}
