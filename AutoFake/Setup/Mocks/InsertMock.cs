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
        private readonly IPrePostProcessor _prePostProcessor;
        private readonly Dictionary<CapturedMember, FieldDefinition> _capturedMembers;

        public InsertMock(IProcessorFactory processorFactory, ClosureDescriptor closure, Location location)
        {
            _processorFactory = processorFactory;
            _prePostProcessor = _processorFactory.CreatePrePostProcessor();
            _location = location;
            Closure = closure;
            _capturedMembers = new Dictionary<CapturedMember, FieldDefinition>();
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

        public void BeforeInjection(MethodDefinition method)
        {
            foreach (var member in Closure.CapturedMembers)
            {
                _capturedMembers[member] = _prePostProcessor.GenerateField(
                    $"Captured_{member.Field.Name}_{Guid.NewGuid()}", member.Instance.GetType());
            }
        }

        [ExcludeFromCodeCoverage]
        public void ProcessInstruction(Instruction instruction)
        {
        }

        public IList<object> Initialize(Type type)
        {
            foreach (var captured in _capturedMembers)
            {
                var field = type.GetField(captured.Value.Name, BindingFlags.NonPublic | BindingFlags.Static)
                    ?? throw new InitializationException($"'{captured.Value.Name}' is not found in the generated object"); ;
                field.SetValue(null, captured.Key.Instance);
            }
            return new List<object>();
        }

        public void Inject(IEmitter emitter, Instruction instruction)
        {
            var processor = _processorFactory.CreateProcessor(emitter, instruction);
            processor.InjectClosure(Closure, beforeInstruction: true, _capturedMembers);
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
