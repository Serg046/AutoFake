﻿using System;
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
            var variables = processor.RecordMethodCall(SetupBodyField, ExecutionContext,
	            SourceMember.GetParameters().Select(p => p.ParameterType).ToList());
			ReplaceInstruction(emitter, processor, instruction, variables);
        }

        private void ReplaceInstruction(IEmitter emitter, IProcessor processor, Instruction instruction,
	        IList<VariableDefinition> variables)
        {
	        var nop = Instruction.Create(OpCodes.Nop);
	        emitter.InsertBefore(instruction, Instruction.Create(OpCodes.Brfalse, nop));
	        if (SourceMember.HasStackInstance) processor.RemoveStackArgument();
	        if (ReturnObject != null)
	        {
		        var opCode = instruction.OpCode == OpCodes.Ldsflda || instruction.OpCode == OpCodes.Ldflda
			        ? OpCodes.Ldsflda
			        : OpCodes.Ldsfld;
		        var retValueFieldRef = ProcessorFactory.TypeInfo.IsMultipleAssembliesMode
			        ? emitter.Body.Method.Module.ImportReference(_retValueField)
			        : _retValueField;
		        emitter.InsertBefore(instruction, Instruction.Create(opCode, retValueFieldRef));
	        }

	        emitter.InsertBefore(instruction, Instruction.Create(OpCodes.Br, instruction.Next));
	        emitter.InsertBefore(instruction, nop);
	        processor.PushMethodArguments(variables);
        }

		public override IList<object> Initialize(Type? type)
        {
            if (type != null)
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
            return new List<object>();
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
