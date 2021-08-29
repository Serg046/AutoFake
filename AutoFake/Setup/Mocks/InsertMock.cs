﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using AutoFake.Exceptions;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup.Mocks
{
    internal class InsertMock : IMock
    {
        private readonly Location _location;
        private readonly IPrePostProcessor _prePostProcessor;
        private readonly IProcessorFactory _processorFactory;
        private FieldDefinition? _closureField;

        public InsertMock(IProcessorFactory processorFactory, Action closure, Location location)
        {
            _prePostProcessor = processorFactory.CreatePrePostProcessor();
            _processorFactory = processorFactory;
            _location = location;
            Closure = closure;
        }

        public Action Closure { get; }

        [ExcludeFromCodeCoverage]
        public void AfterInjection(IEmitter emitter)
        {
        }

        public void BeforeInjection(MethodDefinition method)
        {
            _closureField = _prePostProcessor.GenerateField(
                $"{method.Name}InsertCallback{Guid.NewGuid()}", Closure.GetType());
        }

        public IList<object> Initialize(Type? type)
        {
            if (type != null && _closureField != null)
            {
	            var field = type.GetField(_closureField.Name, BindingFlags.Public | BindingFlags.Static)
	                ?? throw new InitializationException($"'{_closureField.Name}' is not found in the generated object"); ;
	            field.SetValue(null, Closure);
            }
            return new List<object>();
        }

        public void Inject(IEmitter emitter, Instruction instruction)
        {
	        var module = emitter.Body.Method.Module;
	        var closure = _processorFactory.TypeInfo.IsMultipleAssembliesMode
		        ? module.ImportReference(_closureField)
		        : _closureField;
	        emitter.InsertBefore(instruction, Instruction.Create(OpCodes.Ldsfld, closure));
	        emitter.InsertBefore(instruction, Instruction.Create(OpCodes.Call,
		        module.ImportReference(typeof(Action).GetMethod(nameof(Action.Invoke)))));
        }

        public bool IsSourceInstruction(MethodDefinition method, Instruction instruction, IEnumerable<GenericArgument> genericArguments)
        {
            switch (_location)
            {
                case Location.Before: return instruction == method.Body.Instructions.First();
                case Location.After: return instruction == method.Body.Instructions.Last();
                default: return false;
            }
        }

        public enum Location
        {
            Before,
            After
        }
    }
}
