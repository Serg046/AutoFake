using System;
using System.Collections.Generic;
using Mono.Cecil;
using System.Linq;
using AutoFake.Abstractions;
using Mono.Cecil.Cil;
using LinqExpression = System.Linq.Expressions.Expression;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace AutoFake;

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

	public static bool EquivalentTo(this MethodReference methodReference, MethodReference method, bool includeExlicitInterfaceImplementations = false)
		=> EquivalentNames(methodReference.Name, method.Name, includeExlicitInterfaceImplementations) &&
		   methodReference.Parameters.Select(p => p.ParameterType.FullName)
		   .SequenceEqual(method.Parameters.Select(p => p.ParameterType.FullName)) &&
		   methodReference.ReturnType.FullName == method.ReturnType.FullName;

	private static bool EquivalentNames(string nameLeft, string nameRight, bool exlicit) => nameLeft == nameRight || (exlicit && nameLeft.EndsWith($".{nameRight}"));

	public static TypeDefinition ToTypeDefinition(this TypeReference type)
		=> type as TypeDefinition ?? type.Resolve();

	public static FieldDefinition ToFieldDefinition(this FieldReference field)
		=> field as FieldDefinition ?? field.Resolve();

	public static MethodDefinition ToMethodDefinition(this MethodReference method)
		=> method as MethodDefinition ?? method.Resolve();

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

	public static IGenericArgument? FindGenericTypeOrDefault(this IEnumerable<IGenericArgument> genericArguments, string genericParamName)
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
		var attributeName = typeof(StateMachineAttribute).FullName;
		foreach (var attribute in currentMethod.CustomAttributes.Where(a => a.AttributeType.ToTypeDefinition().BaseType?.FullName == attributeName))
		{
			var typeRef = (TypeReference)attribute.ConstructorArguments[0].Value;
			yield return filter(typeRef.ToTypeDefinition().Methods);
		}
	}

	public static bool IsStatic(this Type type) => type.IsAbstract && type.IsSealed;

	public static string GetFullMethodName(this MethodBase method)
		=> method.DeclaringType?.IsInterface == true
		? GetInterfaceName(method) + "." + method.Name
		: method.Name;

	private static string GetInterfaceName(MethodBase method)
	{
		var type = method.DeclaringType.IsConstructedGenericType ? method.DeclaringType.GetGenericTypeDefinition() : method.DeclaringType;
		var typeName = type.ToString().Replace('+', '.').Replace('[', '<').Replace(']', '>');
		typeName = Regex.Replace(typeName, @"(.*)(`\d+)(<.*>)", match => match.Groups[1].Value + match.Groups[3].Value);
		return typeName;
	}
}
