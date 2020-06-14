using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoFake.Exceptions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace AutoFake.Setup.Mocks
{
    internal class InsertMock : IMock
    {
        private readonly Location _location;
        private readonly IProcessorFactory _processorFactory;

        public InsertMock(IProcessorFactory processorFactory, ClosureDescriptor closure, Location location)
        {
            _processorFactory = processorFactory;
            _location = location;
            Closure = closure;
        }

        public ClosureDescriptor Closure { get; }

        public void AfterInjection(IEmitter emitter)
        {
            var type = _processorFactory.TypeInfo.Module.GetType(Closure.DeclaringType, true).Resolve();
            if (type.Attributes.HasFlag(TypeAttributes.NestedPrivate))
            {
                type.Attributes = TypeAttributes.NestedAssembly;
            }
        }

        [ExcludeFromCodeCoverage]
        public void BeforeInjection(MethodDefinition method)
        {
        }

        public IList<object> Initialize(Type type)
        {
            foreach (var captured in Closure.CapturedMembers)
            {
                var field = type.GetField(captured.GeneratedField.Name, BindingFlags.NonPublic | BindingFlags.Static)
                    ?? throw new InitializationException($"'{captured.GeneratedField.Name}' is not found in the generated object"); ;
                field.SetValue(null, captured.Instance);
            }
            return new List<object>();
        }

        public void Inject(IEmitter emitter, Instruction instruction)
        {
            var processor = _processorFactory.CreateProcessor(emitter, instruction);
            processor.InjectClosure(Closure, beforeInstruction: true);
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
