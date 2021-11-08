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
	    private FieldDefinition? _setupBodyField;
	    private FieldDefinition? _executionContext;

	    protected SourceMemberMock(IProcessorFactory processorFactory, IInvocationExpression invocationExpression)
        {
            InvocationExpression = invocationExpression;
            SourceMember = invocationExpression.GetSourceMember();
            PrePostProcessor = processorFactory.CreatePrePostProcessor();
            ProcessorFactory = processorFactory;
        }
        
        protected IProcessorFactory ProcessorFactory { get; }
        protected IPrePostProcessor PrePostProcessor { get; }
        public IInvocationExpression InvocationExpression { get; }
        public ISourceMember SourceMember { get; }
        public Func<uint, bool>? ExpectedCalls { get; set; }

        protected FieldDefinition SetupBodyField => _setupBodyField ?? throw new InvalidOperationException("SetupBody field should be set");
        protected FieldDefinition ExecutionContext => _executionContext ?? throw new InvalidOperationException("ExecutionContext field should be set");

        public virtual void BeforeInjection(MethodDefinition method)
        {
            _setupBodyField = PrePostProcessor.GenerateField(
                GetFieldName(method.Name, nameof(SetupBodyField)), typeof(IInvocationExpression));
            _executionContext = PrePostProcessor.GenerateField(
                GetFieldName(method.Name, nameof(ExecutionContext)), typeof(ExecutionContext));
        }

        public abstract void Inject(IEmitter emitter, Instruction instruction);

        public virtual IList<object> Initialize(Type? type)
        {
            if (type != null)
            {
                var field = GetField(type, SetupBodyField.Name)
                            ?? throw new InitializationException($"'{SetupBodyField.Name}' is not found in the generated object");
                field.SetValue(null, InvocationExpression);

	            var ctxField = GetField(type, ExecutionContext.Name)
	                           ?? throw new InitializationException($"'{ExecutionContext.Name}' is not found in the generated object");
	            ctxField.SetValue(null, new ExecutionContext(ExpectedCalls));
            }

            return new List<object>();
        }

        public bool IsSourceInstruction(MethodDefinition method, Instruction instruction, IEnumerable<GenericArgument> genericArguments)
        {
            return SourceMember.IsSourceInstruction(ProcessorFactory.AssemblyWriter, instruction, genericArguments);
        }

        protected FieldInfo? GetField(Type type, string fieldName)
	        => type.GetField(fieldName, BindingFlags.Public | BindingFlags.Static);

        protected string GetFieldName(string prefix, string suffix)
        {
            var fieldName = new StringBuilder(prefix)
                .Append(SourceMember.ReturnType.FullName).Replace(".", "")
                .Append("_").Append(SourceMember.Name);
            foreach (ParameterInfo parameter in SourceMember.GetParameters())
            {
                fieldName.Append("_").Append(parameter.ParameterType.FullName?.Replace(".", ""));
            }
            return fieldName.Append("_").Append(suffix).ToString();
        }

        public virtual void AfterInjection(IEmitter emitter)
        {
	        PrePostProcessor.InjectVerification(emitter, SetupBodyField, ExecutionContext);
        }
    }
}
