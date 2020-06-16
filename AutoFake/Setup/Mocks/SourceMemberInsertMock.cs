using System;
using System.Collections.Generic;
using System.Reflection;
using AutoFake.Exceptions;
using AutoFake.Expression;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup.Mocks
{
    internal class SourceMemberInsertMock : SourceMemberMock
    {
        private readonly InsertMock.Location _location;
        private FieldDefinition _closureField;

        public SourceMemberInsertMock(IProcessorFactory processorFactory, IInvocationExpression invocationExpression,
            Action closure, InsertMock.Location location) : base(processorFactory, invocationExpression)
        {
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
            var processor = ProcessorFactory.CreateProcessor(emitter, instruction);
            processor.InjectClosure(_closureField, _location);
        }

        public override IList<object> Initialize(Type type)
        {
            var field = type.GetField(_closureField.Name, BindingFlags.NonPublic | BindingFlags.Static)
                        ?? throw new InitializationException($"'{_closureField.Name}' is not found in the generated object"); ;
            field.SetValue(null, Closure);
            return base.Initialize(type);
        }
    }
}
