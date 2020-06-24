using System;
using System.Collections.Generic;
using System.Linq;
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
            CheckArguments = invocationExpression.GetArguments().Any(arg => !(arg.Checker is SuccessfulArgumentChecker));
        }

        protected IProcessorFactory ProcessorFactory { get; }
        protected IPrePostProcessor PrePostProcessor { get; }
        protected FieldDefinition SetupBodyField { get; private set; }
        protected FieldDefinition CallsAccumulator { get; private set; }
        protected FieldDefinition CallsChecker { get; private set; }

        public bool CheckArguments { get; }
        public Func<byte, bool> ExpectedCalls { get; set; }

        public bool CheckSourceMemberCalls => CheckArguments || ExpectedCalls != null;
        public ISourceMember SourceMember { get; }

        public virtual void BeforeInjection(MethodDefinition method)
        {
            if (CheckSourceMemberCalls)
            {
                SetupBodyField = PrePostProcessor.GenerateField(GetFieldName(method.Name, "Setup"), typeof(IInvocationExpression));
                CallsAccumulator = PrePostProcessor.GenerateCallsAccumulator(
                    GetFieldName(method.Name, "CallsAccumulator"), method.Body);
                CallsChecker = PrePostProcessor.GenerateField(GetFieldName(method.Name, "CallsChecker"), typeof(Func<byte, bool>));
            }
        }

        public abstract void Inject(IEmitter emitter, Instruction instruction);

        public virtual IList<object> Initialize(Type type)
        {
            if (SetupBodyField != null)
            {
                var field = GetField(type, SetupBodyField.Name)
                            ?? throw new InitializationException($"'{SetupBodyField.Name}' is not found in the generated object");
                field.SetValue(null, _invocationExpression);
            }
            if (ExpectedCalls != null)
            {
                var field = GetField(type, CallsChecker.Name)
                            ?? throw new InitializationException($"'{CallsChecker.Name}' is not found in the generated object");
                field.SetValue(null, ExpectedCalls);
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

        public virtual void AfterInjection(IEmitter emitter)
        {
            if (CheckSourceMemberCalls)
            {
                PrePostProcessor.InjectVerification(emitter, CheckArguments, CallsChecker,
                    SetupBodyField, CallsAccumulator);
            }
        }
    }
}
