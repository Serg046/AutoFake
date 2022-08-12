﻿using System;
using System.Collections;
using System.Collections.Generic;
using Mono.Cecil;
using System.Linq;
using AutoFake.Abstractions;
using Mono.Cecil.Cil;

namespace AutoFake
{
    internal static class Extensions
    {
        public static bool EquivalentTo(this MethodReference methodReference, MethodReference method)
            => methodReference.Name == method.Name &&
               methodReference.Parameters.Select(p => p.ParameterType.FullName)
         .SequenceEqual(method.Parameters.Select(p => p.ParameterType.FullName)) &&
               methodReference.ReturnType.FullName == method.ReturnType.FullName;

        public static TypeDefinition ToTypeDefinition(this TypeReference type)
            => type as TypeDefinition ?? type.Resolve();

        public static FieldDefinition ToFieldDefinition(this FieldReference field)
            => field as FieldDefinition ?? field.Resolve();

        public static MethodDefinition ToMethodDefinition(this MethodReference method)
            => method as MethodDefinition ?? method.Resolve();

        public static IEqualityComparer ToNonGeneric<T>(this IEqualityComparer<T?> comparer)
            => new EqualityComparer((x, y) => comparer.Equals((T?)x, (T?)y), x => comparer.GetHashCode((T)x));

        public static Instruction Copy(this Instruction instruction)
        {
	        if (instruction is null) throw new ArgumentNullException(nameof(instruction));
            if (instruction.Operand is null) return Instruction.Create(instruction.OpCode);

            return instruction.Operand switch
            {
	            TypeReference operand => Instruction.Create(instruction.OpCode, operand),
	            CallSite operand => Instruction.Create(instruction.OpCode, operand),
	            MethodReference operand => Instruction.Create(instruction.OpCode, operand),
	            FieldReference operand => Instruction.Create(instruction.OpCode, operand),
	            string operand => Instruction.Create(instruction.OpCode, operand),
	            sbyte operand => Instruction.Create(instruction.OpCode, operand),
	            byte operand => Instruction.Create(instruction.OpCode, operand),
	            int operand => Instruction.Create(instruction.OpCode, operand),
	            long operand => Instruction.Create(instruction.OpCode, operand),
	            float operand => Instruction.Create(instruction.OpCode, operand),
	            double operand => Instruction.Create(instruction.OpCode, operand),
	            Instruction operand => Instruction.Create(instruction.OpCode, operand),
	            Instruction[] operand => Instruction.Create(instruction.OpCode, operand),
	            VariableDefinition operand => Instruction.Create(instruction.OpCode, operand),
	            ParameterDefinition operand => Instruction.Create(instruction.OpCode, operand),
	            _ => throw new NotSupportedException("The operand is not supported")
            };
        }

        public static Instruction ShiftDown(this IEmitter emitter, Instruction instruction)
        {
	        var copy = instruction.Copy();
	        instruction.OpCode = OpCodes.Nop;
	        instruction.Operand = null;
	        emitter.InsertAfter(instruction, copy);
	        return copy;
        }

        public static GenericArgument? FindGenericTypeOrDefault(this IEnumerable<GenericArgument> genericArguments, string genericParamName)
        {
            string? prevType = null;
	        foreach (var genericArgument in genericArguments)
	        {
		        if (genericArgument.Name == genericParamName)
		        {
			        if (genericArgument.GenericDeclaringType != null)
			        {
				        genericParamName = genericArgument.Type;
				        prevType = genericArgument.GenericDeclaringType;
			        }
			        else if (prevType == null || prevType == genericArgument.DeclaringType)
			        {
						return genericArgument;
                    }
                }
	        }

	        return null;
        }

		public static IEnumerable<MethodDefinition> GetStateMachineMethods(this MethodDefinition currentMethod, Func<ICollection<MethodDefinition>, MethodDefinition> filter)
		{
			foreach (var attribute in currentMethod.CustomAttributes.Where(a => a.AttributeType.Name.EndsWith("StateMachineAttribute")
				&& a.AttributeType.ToTypeDefinition().BaseType?.FullName == "System.Runtime.CompilerServices.StateMachineAttribute"))
			{
				var typeRef = (TypeReference)attribute.ConstructorArguments[0].Value;
				yield return filter(typeRef.ToTypeDefinition().Methods);
			}
		}

		private class EqualityComparer : IEqualityComparer
        {
            private readonly Func<object?, object?, bool> _comparer;
            private readonly Func<object, int> _hasher;

            public EqualityComparer(Func<object?, object?, bool> comparer, Func<object, int> hasher)
            {
                _comparer = comparer;
                _hasher = hasher;
            }

            bool IEqualityComparer.Equals(object? x, object? y) => _comparer(x, y);
            int IEqualityComparer.GetHashCode(object obj) => _hasher(obj);
        }
    }
}
