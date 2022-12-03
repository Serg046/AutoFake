using System;
using System.Collections.Generic;
using AutoFake.Abstractions;
using AutoFake.Abstractions.Setup.Mocks;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup.Mocks
{
	internal class VerifyMock : IVerifyMock
	{
		private readonly Func<IEmitter, Instruction, IProcessor> _createProcessor;

		public VerifyMock(
			ISourceMemberMetaData sourceMemberMetaData,
			Func<IEmitter, Instruction, IProcessor> createProcessor)
		{
			SourceMemberMetaData = sourceMemberMetaData;
			_createProcessor = createProcessor;
		}

		public ISourceMemberMetaData SourceMemberMetaData { get; }

		public bool IsSourceInstruction(MethodDefinition method, Instruction instruction, IEnumerable<GenericArgument> genericArguments)
		{
			return SourceMemberMetaData.SourceMember.IsSourceInstruction(instruction, genericArguments);
		}

		public void BeforeInjection(MethodDefinition method)
		{
			SourceMemberMetaData.BeforeInjection(method);
		}

		public void Inject(IEmitter emitter, Instruction instruction)
		{
			var processor = _createProcessor(emitter, instruction);
			var arguments = SourceMemberMetaData.RecordMethodCall(processor);
			emitter.InsertBefore(instruction, Instruction.Create(OpCodes.Pop));
			processor.PushMethodArguments(arguments);
		}

		public void AfterInjection(IEmitter emitter)
		{
			SourceMemberMetaData.AfterInjection(emitter);
		}

		public void Initialize(Type? type)
		{
			SourceMemberMetaData.Initialize(type);
		}
	}
}
