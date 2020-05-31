using System;
using System.Collections.Generic;
using System.Reflection;
using AutoFake.Exceptions;
using AutoFake.Expression;
using Mono.Cecil;
using Mono.Cecil.Cil;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace AutoFake.Setup.Mocks
{
    internal class SourceMemberInsertMock : SourceMemberMock
    {
        private readonly InsertMock.Location _location;
        private readonly Dictionary<CapturedMember, FieldDefinition> _capturedMembers;

        public SourceMemberInsertMock(IProcessorFactory processorFactory, IInvocationExpression invocationExpression,
            ClosureDescriptor closure, InsertMock.Location location) : base(processorFactory, invocationExpression)
        {
            _location = location;
            Closure = closure;
            _capturedMembers = new Dictionary<CapturedMember, FieldDefinition>();
        }

        public ClosureDescriptor Closure { get; }

        public override void BeforeInjection(MethodDefinition method)
        {
            base.BeforeInjection(method);
            foreach (var member in Closure.CapturedMembers)
            {
                _capturedMembers[member] = PrePostProcessor.GenerateField(
                    GetFieldName(member.Field.Name, "Captured"), member.Instance.GetType());
            }
        }

        [ExcludeFromCodeCoverage]
        public override void ProcessInstruction(Instruction instruction)
        {
        }

        public override void Inject(IEmitter emitter, Instruction instruction)
        {
            var processor = ProcessorFactory.CreateProcessor(emitter, instruction);
            processor.InjectClosure(Closure, beforeInstruction: _location == InsertMock.Location.Top, _capturedMembers);
        }

        public override void AfterInjection(IEmitter emitter)
        {
            base.AfterInjection(emitter);
            var type = ProcessorFactory.TypeInfo.Module.GetType(Closure.DeclaringType, true).Resolve();
            if (type.Attributes.HasFlag(TypeAttributes.NestedPrivate))
            {
                type.Attributes = TypeAttributes.NestedAssembly;
            }
        }

        public override IList<object> Initialize(Type type)
        {
            foreach (var captured in _capturedMembers)
            {
                var field = type.GetField(captured.Value.Name, BindingFlags.NonPublic | BindingFlags.Static)
                            ?? throw new FakeGeneretingException($"'{captured.Value.Name}' is not found in the generated object"); ;
                field.SetValue(null, captured.Key.Instance);
            }
            return base.Initialize(type);
        }
    }
}
