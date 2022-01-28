using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup.Mocks
{
    internal class SourceMemberInsertMock : ISourceMemberMock
    {
	    private readonly ICecilFactory _cecilFactory;
	    private readonly InsertMock.Location _location;
	    private readonly Func<IEmitter, Instruction, IProcessor> _createProcessor;
	    private readonly ITypeInfo _typeInfo;
	    private FieldDefinition? _closureField;

        public SourceMemberInsertMock(
	        SourceMemberMetaData sourceMemberMetaData,
            ICecilFactory cecilFactory,
            Action closure, InsertMock.Location location,
            Func<IEmitter, Instruction, IProcessor> createProcessor,
			ITypeInfo typeInfo)
        {
	        SourceMemberMetaData = sourceMemberMetaData;
	        _cecilFactory = cecilFactory;
	        _location = location;
	        _createProcessor = createProcessor;
	        _typeInfo = typeInfo;
	        Closure = closure;
        }

	    public SourceMemberMetaData SourceMemberMetaData { get; }
        public Action Closure { get; }

        public bool IsSourceInstruction(MethodDefinition method, Instruction instruction, IEnumerable<GenericArgument> genericArguments)
        {
	        return SourceMemberMetaData.SourceMember.IsSourceInstruction(instruction, genericArguments);
        }

        public void BeforeInjection(MethodDefinition method)
        {
	        SourceMemberMetaData.BeforeInjection(method);
            _closureField = SourceMemberMetaData.PrePostProcessor.GenerateField(
                $"{method.Name}InsertCallback{Guid.NewGuid()}", Closure.GetType());
        }

        public void Inject(IEmitter emitter, Instruction instruction)
        {
	        if (_closureField == null) throw new InvalidOperationException("Closure field should be set");
	        var module = emitter.Body.Method.Module;
	        var closureRef = _typeInfo.IsMultipleAssembliesMode
		        ? module.ImportReference(_closureField)
		        : _closureField;
            var processor = _createProcessor(emitter, instruction);
            var variables = processor.RecordMethodCall(SourceMemberMetaData.SetupBodyField, SourceMemberMetaData.ExecutionContext,
	            SourceMemberMetaData.SourceMember.GetParameters().Select(p => p.ParameterType).ToReadOnlyList());
            var verifyVar = _cecilFactory.CreateVariable(module.TypeSystem.Boolean);
            emitter.Body.Variables.Add(verifyVar);
            emitter.InsertBefore(instruction, Instruction.Create(OpCodes.Stloc, verifyVar));
            if (_location == InsertMock.Location.Before)
            {
				emitter.InsertBefore(instruction, Instruction.Create(OpCodes.Ldloc, verifyVar));
	            InjectBefore(emitter, instruction, module, closureRef);
            }
            else
            {
	            InjectAfter(emitter, instruction, module, closureRef);
				emitter.InsertAfter(instruction, Instruction.Create(OpCodes.Ldloc, verifyVar));
            }
            processor.PushMethodArguments(variables);
        }

        public void AfterInjection(IEmitter emitter)
        {
	        SourceMemberMetaData.AfterInjection(emitter);
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

        public void Initialize(Type? type)
        {
	        SourceMemberMetaData.Initialize(type);
            InsertMock.InitializeClosure(type, _closureField, Closure);
        }
    }
}
