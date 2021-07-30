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
        protected SourceMemberMock(IProcessorFactory processorFactory, IInvocationExpression invocationExpression)
        {
            InvocationExpression = invocationExpression;
            SourceMember = invocationExpression.GetSourceMember();
            PrePostProcessor = processorFactory.CreatePrePostProcessor();
            ProcessorFactory = processorFactory;
        }
        
        protected IProcessorFactory ProcessorFactory { get; }
        protected IPrePostProcessor PrePostProcessor { get; }
        protected FieldDefinition SetupBodyField { get; private set; }
        protected FieldDefinition ExecutionContext { get; private set; }

        public IInvocationExpression InvocationExpression { get; set; }
        public Func<uint, bool> ExpectedCalls { get; set; }
        public ISourceMember SourceMember { get; }

        public virtual void BeforeInjection(MethodDefinition method)
        {
            SetupBodyField = PrePostProcessor.GenerateField(
                GetFieldName(method.Name, nameof(SetupBodyField)), typeof(IInvocationExpression));
            ExecutionContext = PrePostProcessor.GenerateField(
                GetFieldName(method.Name, nameof(ExecutionContext)), typeof(ExecutionContext));
        }

        public abstract void Inject(IEmitter emitter, Instruction instruction);

        public virtual IList<object> Initialize(Type? type)
        {
            if (type != null)
            {
	            if (SetupBodyField != null)
	            {
	                var field = GetField(type, SetupBodyField.Name)
	                            ?? throw new InitializationException($"'{SetupBodyField.Name}' is not found in the generated object");
	                field.SetValue(null, InvocationExpression);
	            }
            }

            var ctxField = GetField(type, ExecutionContext.Name)
                           ?? throw new InitializationException($"'{ExecutionContext.Name}' is not found in the generated object");
            ctxField.SetValue(null, new ExecutionContext(ExpectedCalls));

            return new List<object>();
        }

        public bool IsSourceInstruction(MethodDefinition method, Instruction instruction)
        {
            return SourceMember.IsSourceInstruction(ProcessorFactory.TypeInfo, instruction);
        }

        protected FieldInfo GetField(Type type, string fieldName)
            => type.GetField(fieldName, BindingFlags.Public | BindingFlags.Static);

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
	        PrePostProcessor.InjectVerification(emitter, SetupBodyField, ExecutionContext);
        }
    }
}
