using System;
using System.Linq;
using AutoFake.Expression;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup.Mocks
{
    internal class SourceMemberInsertMock : SourceMemberMock
    {
	    private readonly ICecilFactory _cecilFactory;
	    private readonly InsertMock.Location _location;
        private FieldDefinition? _closureField;

        public SourceMemberInsertMock(
	        IProcessorFactory processorFactory,
	        ICecilFactory cecilFactory,
	        IInvocationExpression invocationExpression,
	        IExecutionContext.Create getExecutionContext,
            Action closure, InsertMock.Location location) : base(processorFactory, getExecutionContext, invocationExpression)
        {
	        _cecilFactory = cecilFactory;
	        _location = location;
            Closure = closure;
        }

        public Action Closure { get; }

        public override void BeforeInjection(MethodDefinition method)
        {
            base.BeforeInjection(method);
            _closureField = PrePostProcessor.GenerateField(
                $"{method.Name}InsertCallback{Guid.NewGuid()}", Closure.GetType());
        }

        public override void Inject(IEmitter emitter, Instruction instruction)
        {
	        if (_closureField == null) throw new InvalidOperationException("Closure field should be set");
	        var module = emitter.Body.Method.Module;
	        var closureRef = ProcessorFactory.TypeInfo.IsMultipleAssembliesMode
		        ? module.ImportReference(_closureField)
		        : _closureField;
            var processor = ProcessorFactory.CreateProcessor(emitter, instruction);
            var variables = processor.RecordMethodCall(SetupBodyField, ExecutionContext,
	            SourceMember.GetParameters().Select(p => p.ParameterType).ToReadOnlyList());
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

        public override void Initialize(Type? type)
        {
			base.Initialize(type);
            InsertMock.InitializeClosure(type, _closureField, Closure);
        }
    }
}
