using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using AutoFake.Abstractions;
using AutoFake.Abstractions.Expression;
using AutoFake.Abstractions.Setup;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup.Mocks
{
	internal class SourceMemberMetaData
	{
		private readonly IExecutionContext.Create _getExecutionContext;
		private FieldDefinition? _setupBodyField;
		private FieldDefinition? _executionContext;

		protected SourceMemberMetaData(
			IExecutionContext.Create getExecutionContext,
			IInvocationExpression invocationExpression,
			IPrePostProcessor prePostProcessor)
		{
			_getExecutionContext = getExecutionContext;
			InvocationExpression = invocationExpression;
			SourceMember = invocationExpression.GetSourceMember();
			PrePostProcessor = prePostProcessor;
		}

		public IPrePostProcessor PrePostProcessor { get; }
		public IInvocationExpression InvocationExpression { get; }
		public ISourceMember SourceMember { get; }
		public IExecutionContext.CallsCheckerFunc? ExpectedCalls { get; set; }
		public Func<bool>? WhenFunc { get; set; }
		private FieldDefinition SetupBodyField => _setupBodyField ?? throw new InvalidOperationException("SetupBody field should be set");
		private FieldDefinition ExecutionContext => _executionContext ?? throw new InvalidOperationException("ExecutionContext field should be set");

		public void BeforeInjection(MethodDefinition method)
		{
			_setupBodyField = PrePostProcessor.GenerateField(
				GetFieldName(method.Name, nameof(SetupBodyField)), typeof(IInvocationExpression));
			_executionContext = PrePostProcessor.GenerateField(
				GetFieldName(method.Name, nameof(ExecutionContext)), typeof(IExecutionContext));
		}

		public void Initialize(Type? type)
		{
			if (type != null)
			{
				var field = GetField(type, SetupBodyField.Name) ?? throw new MissingFieldException($"'{SetupBodyField.Name}' is not found");
				field.SetValue(null, InvocationExpression);
				var ctxField = GetField(type, ExecutionContext.Name) ?? throw new MissingFieldException($"'{ExecutionContext.Name}' is not found");
				ctxField.SetValue(null, _getExecutionContext(ExpectedCalls, WhenFunc));
			}
		}

		public FieldInfo? GetField(Type type, string fieldName) => type.GetField(fieldName, BindingFlags.Public | BindingFlags.Static);

		public string GetFieldName(string prefix, string suffix)
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

		public void AfterInjection(IEmitter emitter)
		{
			PrePostProcessor.InjectVerification(emitter, SetupBodyField, ExecutionContext);
		}

		public IReadOnlyList<VariableDefinition> RecordMethodCall(IProcessor processor)
		{
			return processor.RecordMethodCall(SetupBodyField, ExecutionContext, SourceMember.GetParameters().Select(p => p.ParameterType).ToReadOnlyList());
		}
	}
}
