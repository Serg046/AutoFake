using System;
using System.Collections.Generic;
using AutoFake.Abstractions;
using AutoFake.Abstractions.Setup.Mocks;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup.Mocks
{
	internal class ReplaceMock : IReplaceMock
	{
		private readonly Func<IEmitter, Instruction, IProcessor> _createProcessor;
		private readonly ITypeInfo _typeInfo;
		private readonly Lazy<bool> _hasReturnType;
		private FieldDefinition? _retValueField;

		public ReplaceMock(
			ISourceMemberMetaData sourceMemberMetaData,
			Func<IEmitter, Instruction, IProcessor> createProcessor,
			ITypeInfo typeInfo)
		{
			SourceMemberMetaData = sourceMemberMetaData;
			_createProcessor = createProcessor;
			_typeInfo = typeInfo;
			_hasReturnType = new(() => SourceMemberMetaData.SourceMember.ReturnType != typeof(void));
		}

		public ISourceMemberMetaData SourceMemberMetaData { get; }
		public object? ReturnObject { get; set; }

		public bool IsSourceInstruction(MethodDefinition method, Instruction instruction, IEnumerable<IGenericArgument> genericArguments)
		{
			return SourceMemberMetaData.SourceMember.IsSourceInstruction(instruction, genericArguments);
		}

		public void BeforeInjection(MethodDefinition method)
		{
			SourceMemberMetaData.BeforeInjection(method);
			if (_hasReturnType.Value)
			{
				_retValueField = SourceMemberMetaData.PrePostProcessor.GenerateField(SourceMemberMetaData.GetFieldName(method.Name, "RetValue"),
					SourceMemberMetaData.SourceMember.ReturnType);
			}
		}

		public void Inject(IEmitter emitter, Instruction instruction)
		{
			var processor = _createProcessor(emitter, instruction);
			var arguments = SourceMemberMetaData.RecordMethodCall(processor);
			ReplaceInstruction(emitter, processor, instruction, arguments);
		}

		public void AfterInjection(IEmitter emitter)
		{
			SourceMemberMetaData.AfterInjection(emitter);
		}

		private void ReplaceInstruction(IEmitter emitter, IProcessor processor, Instruction instruction,
			IEnumerable<VariableDefinition> variables)
		{
			var nop = Instruction.Create(OpCodes.Nop);
			emitter.InsertBefore(instruction, Instruction.Create(OpCodes.Brfalse, nop));
			if (SourceMemberMetaData.SourceMember.HasStackInstance) processor.RemoveStackArgument();
			if (_hasReturnType.Value)
			{
				var opCode = instruction.OpCode == OpCodes.Ldsflda || instruction.OpCode == OpCodes.Ldflda
					? OpCodes.Ldsflda
					: OpCodes.Ldsfld;
				var retValueFieldRef = _typeInfo.IsMultipleAssembliesMode
					? emitter.Body.Method.Module.ImportReference(_retValueField)
					: _retValueField;
				emitter.InsertBefore(instruction, Instruction.Create(opCode, retValueFieldRef));
			}

			emitter.InsertBefore(instruction, Instruction.Create(OpCodes.Br, instruction.Next));
			emitter.InsertBefore(instruction, nop);
			processor.PushMethodArguments(variables);
		}

		public void Initialize(Type? type)
		{
			SourceMemberMetaData.Initialize(type);
			if (type != null && _hasReturnType.Value && _retValueField != null)
			{
				var field = SourceMemberMetaData.GetField(type, _retValueField.Name)
							?? throw new MissingFieldException($"'{_retValueField.Name}' is not found");
				field.SetValue(null, ReturnObject);
			}
		}
	}
}
