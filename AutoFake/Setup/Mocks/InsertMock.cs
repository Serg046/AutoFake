using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup.Mocks
{
    internal class InsertMock : IMock
    {
        private readonly Location _location;
        private readonly IProcessorFactory _processorFactory;

        public InsertMock(IProcessorFactory processorFactory, MethodDescriptor action, Location location)
        {
            _processorFactory = processorFactory;
            _location = location;
            Action = action;
        }

        public MethodDescriptor Action { get; }

        [ExcludeFromCodeCoverage]
        public void AfterInjection(IEmitter emitter)
        {
            var type = _processorFactory.TypeInfo.Module.GetType(Action.DeclaringType, true).Resolve();
            if (type.Attributes.HasFlag(TypeAttributes.NestedPrivate))
            {
                type.Attributes = TypeAttributes.NestedAssembly;
            }
        }

        [ExcludeFromCodeCoverage]
        public void BeforeInjection(MethodDefinition method)
        {
        }

        [ExcludeFromCodeCoverage]
        public void ProcessInstruction(Instruction instruction)
        {
        }

        public IList<object> Initialize(Type type)
        {
            return new List<object>();
        }

        public void Inject(IEmitter emitter, Instruction instruction)
        {
            var processor = _processorFactory.CreateProcessor(emitter, instruction);
            processor.InjectCallback(Action, beforeInstruction: true);
        }

        public bool IsSourceInstruction(MethodDefinition method, Instruction instruction)
        {
            switch (_location)
            {
                case Location.Top: return instruction == method.Body.Instructions.First();
                case Location.Bottom: return instruction == method.Body.Instructions.Last();
                default: return false;
            }
        }

        public enum Location
        {
            Top,
            Bottom
        }
    }
}
