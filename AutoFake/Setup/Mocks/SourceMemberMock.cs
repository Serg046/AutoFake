using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using AutoFake.Exceptions;
using AutoFake.Expression;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup.Mocks
{
    internal abstract class SourceMemberMock : IMock
    {
        private readonly IInvocationExpression _invocationExpression;

        protected SourceMemberMock(IProcessorFactory processorFactory, IInvocationExpression invocationExpression)
        {
            _invocationExpression = invocationExpression;
            SourceMember = invocationExpression.GetSourceMember();
            PrePostProcessor = processorFactory.CreatePrePostProcessor();
            ProcessorFactory = processorFactory;
        }

        protected IProcessorFactory ProcessorFactory { get; }
        protected IPrePostProcessor PrePostProcessor { get; }
        protected FieldDefinition SetupBodyField { get; private set; }
        protected VariableDefinition CallsAccumulator { get; private set; }

        public bool CheckArguments { get; set; }
        public MethodDescriptor ExpectedCalls { get; set; }

        public bool CheckSourceMemberCalls => CheckArguments || ExpectedCalls != null;
        public ISourceMember SourceMember { get; }

        public virtual void BeforeInjection(MethodDefinition method)
        {
            if (CheckSourceMemberCalls)
            {
                SetupBodyField = PrePostProcessor.GenerateSetupBodyField(GetFieldName(method.Name, "Setup"));
                CallsAccumulator = PrePostProcessor.GenerateCallsAccumulator(method.Body);
            }
        }

        public abstract void Inject(IEmitter emitter, Instruction instruction);

        public virtual IList<object> Initialize(Type type)
        {
            if (SetupBodyField != null)
            {
                var field = GetField(type, SetupBodyField.Name);
                if (field == null) throw new FakeGeneretingException($"'{SetupBodyField.Name}' is not found in the generated object");
                field.SetValue(null, _invocationExpression);
            }
            return new List<object>();
        }

        public bool IsSourceInstruction(MethodDefinition method, Instruction instruction)
        {
            return SourceMember.IsSourceInstruction(ProcessorFactory.TypeInfo, instruction);
        }

        protected FieldInfo GetField(Type type, string fieldName)
            => type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);

        protected string GetFieldName(string prefix, string suffix)
        {
            var fieldName = new StringBuilder(prefix)
                .Append(SourceMember.ReturnType.FullName).Replace(".", "")
                .Append("_").Append(SourceMember.Name);
            foreach (var parameter in SourceMember.GetParameters())
            {
                fieldName.Append("_").Append(parameter.ParameterType.FullName.Replace(".", ""));
            }
            return fieldName.Append("_").Append(suffix).ToString();
        }

        public void AfterInjection(IEmitter emitter)
        {
            if (CheckSourceMemberCalls)
            {
                PrePostProcessor.InjectVerification(emitter, CheckArguments, ExpectedCalls,
                    SetupBodyField, CallsAccumulator);
            }
        }
    }
}
