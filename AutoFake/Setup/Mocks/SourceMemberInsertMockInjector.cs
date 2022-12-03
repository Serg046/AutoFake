using System;
using AutoFake.Abstractions;
using AutoFake.Abstractions.Setup.Mocks;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup.Mocks
{
	internal class SourceMemberInsertMockInjector : ISourceMemberInsertMockInjector
	{
		private readonly SourceMemberMetaData _sourceMemberMetaData;
		private readonly ICecilFactory _cecilFactory;
		private readonly Func<IEmitter, Instruction, IProcessor> _createProcessor;

		public SourceMemberInsertMockInjector(
			SourceMemberMetaData sourceMemberMetaData,
			ICecilFactory cecilFactory,
			Func<IEmitter, Instruction, IProcessor> createProcessor)
		{
			_sourceMemberMetaData = sourceMemberMetaData;
			_cecilFactory = cecilFactory;
			_createProcessor = createProcessor;
		}

		public void Inject(IEmitter emitter, Instruction instruction, FieldReference closureField, IInsertMock.Location location)
		{
			var module = emitter.Body.Method.Module;
			var processor = _createProcessor(emitter, instruction);
			var arguments = _sourceMemberMetaData.RecordMethodCall(processor);
			var verifyVar = _cecilFactory.CreateVariable(module.TypeSystem.Boolean);
			emitter.Body.Variables.Add(verifyVar);
			emitter.InsertBefore(instruction, Instruction.Create(OpCodes.Stloc, verifyVar));
			if (location == IInsertMock.Location.Before)
			{
				emitter.InsertBefore(instruction, Instruction.Create(OpCodes.Ldloc, verifyVar));
				InjectBefore(emitter, instruction, module, closureField);
			}
			else
			{
				InjectAfter(emitter, instruction, module, closureField);
				emitter.InsertAfter(instruction, Instruction.Create(OpCodes.Ldloc, verifyVar));
			}
			processor.PushMethodArguments(arguments);
		}

		private void InjectBefore(IEmitter emitter, Instruction instruction, ModuleDefinition module, FieldReference closure)
		{
			var nop = Instruction.Create(OpCodes.Nop);
			emitter.InsertBefore(instruction, Instruction.Create(OpCodes.Brfalse, nop));
			emitter.InsertBefore(instruction, Instruction.Create(OpCodes.Ldsfld, closure));
			emitter.InsertBefore(instruction, InsertMock.CreateActionInvokeInstruction(module));
			emitter.InsertBefore(instruction, nop);
		}

		private void InjectAfter(IEmitter emitter, Instruction instruction, ModuleDefinition module, FieldReference closure)
		{
			var nop = Instruction.Create(OpCodes.Nop);
			emitter.InsertAfter(instruction, nop);
			emitter.InsertAfter(instruction, InsertMock.CreateActionInvokeInstruction(module));
			emitter.InsertAfter(instruction, Instruction.Create(OpCodes.Ldsfld, closure));
			emitter.InsertAfter(instruction, Instruction.Create(OpCodes.Brfalse, nop));
		}
	}
}
