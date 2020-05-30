using AutoFake.Expression;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup.Mocks
{
    internal class SourceMemberInsertMock : SourceMemberMock
    {
        private readonly InsertMock.Location _location;

        public SourceMemberInsertMock(IProcessorFactory processorFactory, IInvocationExpression invocationExpression,
            MethodDescriptor action, InsertMock.Location location) : base(processorFactory, invocationExpression)
        {
            _location = location;
            Action = action;
        }

        public MethodDescriptor Action { get; }

        [ExcludeFromCodeCoverage]
        public override void ProcessInstruction(Instruction instruction)
        {
        }

        public override void Inject(IEmitter emitter, Instruction instruction)
        {
            var processor = ProcessorFactory.CreateProcessor(emitter, instruction);
            processor.InjectCallback(Action, beforeInstruction: _location == InsertMock.Location.Top);
        }

        public override void AfterInjection(IEmitter emitter)
        {
            base.AfterInjection(emitter);
            var type = ProcessorFactory.TypeInfo.Module.GetType(Action.DeclaringType, true).Resolve();
            if (type.Attributes.HasFlag(TypeAttributes.NestedPrivate))
            {
                type.Attributes = TypeAttributes.NestedAssembly;
            }
        }
    }
}
