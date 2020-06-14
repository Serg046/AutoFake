using System;
using System.Collections.Generic;
using System.Reflection;
using AutoFake.Exceptions;
using AutoFake.Expression;
using Mono.Cecil.Cil;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace AutoFake.Setup.Mocks
{
    internal class SourceMemberInsertMock : SourceMemberMock
    {
        private readonly InsertMock.Location _location;

        public SourceMemberInsertMock(IProcessorFactory processorFactory, IInvocationExpression invocationExpression,
            ClosureDescriptor closure, InsertMock.Location location) : base(processorFactory, invocationExpression)
        {
            _location = location;
            Closure = closure;
        }

        public ClosureDescriptor Closure { get; }
        
        public override void Inject(IEmitter emitter, Instruction instruction)
        {
            var processor = ProcessorFactory.CreateProcessor(emitter, instruction);
            processor.InjectClosure(Closure, beforeInstruction: _location == InsertMock.Location.Top);
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
            foreach (var captured in Closure.CapturedMembers)
            {
                var field = type.GetField(captured.GeneratedField.Name, BindingFlags.NonPublic | BindingFlags.Static)
                            ?? throw new InitializationException($"'{captured.GeneratedField.Name}' is not found in the generated object"); ;
                field.SetValue(null, captured.Instance);
            }
            return base.Initialize(type);
        }
    }
}
