using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoFake.Abstractions;
using AutoFake.Abstractions.Setup.Mocks;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup.Mocks
{
	internal class InsertMock : IMock
	{
		private readonly Location _location;
		private readonly IPrePostProcessor _prePostProcessor;
		private readonly ITypeInfo _typeInfo;
		private FieldDefinition? _closureField;

		public InsertMock(Action closure, Location location, IPrePostProcessor prePostProcessor, ITypeInfo typeInfo)
		{
			_prePostProcessor = prePostProcessor;
			_typeInfo = typeInfo;
			_location = location;
			Closure = closure;
		}

		public Action Closure { get; }

		public void AfterInjection(IEmitter emitter)
		{
		}

		public void BeforeInjection(MethodDefinition method)
		{
			_closureField = _prePostProcessor.GenerateField(
				$"{method.Name}InsertCallback{Guid.NewGuid()}", Closure.GetType());
		}

		public void Initialize(Type? type)
		{
			InitializeClosure(type, _closureField, Closure);
		}

		internal static void InitializeClosure(Type? type, FieldDefinition? closureField, Action closure)
		{
			if (type != null && closureField != null)
			{
				var field = type.GetField(closureField.Name, BindingFlags.Public | BindingFlags.Static)
							?? throw new MissingFieldException($"'{closureField.Name}' is not found");
				field.SetValue(null, closure);
			}
		}

		public void Inject(IEmitter emitter, Instruction instruction)
		{
			var module = emitter.Body.Method.Module;
			var closure = _typeInfo.IsMultipleAssembliesMode
				? module.ImportReference(_closureField)
				: _closureField;
			emitter.InsertBefore(instruction, Instruction.Create(OpCodes.Ldsfld, closure));
			emitter.InsertBefore(instruction, CreateActionInvokeInstruction(module));
		}

		internal static Instruction CreateActionInvokeInstruction(ModuleDefinition module)
			=> Instruction.Create(OpCodes.Call, module.ImportReference(typeof(Action).GetMethod(nameof(Action.Invoke))));

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
