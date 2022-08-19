using System;
using System.Collections;
using System.Collections.Generic;
using Mono.Cecil;
using System.Linq;
using AutoFake.Abstractions;
using Mono.Cecil.Cil;
using LinqExpression = System.Linq.Expressions.Expression;
using System.Reflection;

namespace AutoFake
{
	internal static class Extensions
	{
		private static readonly Func<OpCode, object, Instruction> _createinstruction;

		static Extensions()
		{
			var ctor = typeof(Instruction)
				.GetConstructor(BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.Instance,
				null, new[] { typeof(OpCode), typeof(object) }, null);
			var opCode = LinqExpression.Parameter(typeof(OpCode));
			var operand = LinqExpression.Parameter(typeof(object));
			_createinstruction = LinqExpression.Lambda<Func<OpCode, object, Instruction>>(
				LinqExpression.New(ctor, opCode, operand), opCode, operand).Compile();
		}

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
			return instruction.Operand == null
				? Instruction.Create(instruction.OpCode)
				: _createinstruction(instruction.OpCode, instruction.Operand);
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

		public static bool IsStatic(this Type type) => type.IsAbstract && type.IsSealed;

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
