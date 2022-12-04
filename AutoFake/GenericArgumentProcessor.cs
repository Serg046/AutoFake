using System.Collections.Generic;
using AutoFake.Abstractions;
using Mono.Cecil;

namespace AutoFake;

internal class GenericArgumentProcessor : IGenericArgumentProcessor
{
	private readonly IGenericArgument.Create _createGenericArgument;

	public GenericArgumentProcessor(IGenericArgument.Create createGenericArgument)
	{
		_createGenericArgument = createGenericArgument;
	}

	public IEnumerable<IGenericArgument> GetGenericArguments(FieldReference fieldRef)
	{
		var fieldDef = fieldRef.ToFieldDefinition();
		foreach (var arg in GetGenericArguments(fieldRef.DeclaringType, fieldDef.DeclaringType))
		{
			yield return arg;
		}
	}

	public IEnumerable<IGenericArgument> GetGenericArguments(MethodReference methodRef, MethodDefinition methodDef)
	{
		if (methodRef is GenericInstanceMethod genericInstanceMethod)
		{
			for (var i = 0; i < genericInstanceMethod.GenericArguments.Count; i++)
			{
				var genericArgument = genericInstanceMethod.GenericArguments[i];
				var declaringType = methodDef.DeclaringType.ToString();
				yield return _createGenericArgument(
					methodDef.GenericParameters[i].Name,
					genericArgument.ToString(),
					declaringType,
					GetGenericDeclaringType(genericArgument as GenericParameter));
			}
		}

		foreach (var arg in GetGenericArguments(methodRef.DeclaringType, methodDef.DeclaringType))
		{
			yield return arg;
		}
	}

	private IEnumerable<IGenericArgument> GetGenericArguments(TypeReference typeRef, TypeDefinition typeDef)
	{
		if (typeRef is GenericInstanceType genericInstanceType)
		{
			for (var i = 0; i < genericInstanceType.GenericArguments.Count; i++)
			{
				var genericArgument = genericInstanceType.GenericArguments[i];
				var declaringType = typeDef.ToString();
				yield return _createGenericArgument(
					typeDef.GenericParameters[i].Name,
					genericArgument.ToString(),
					declaringType,
					GetGenericDeclaringType(genericArgument as GenericParameter));
			}
		}
	}

	private string? GetGenericDeclaringType(GenericParameter? genericArgument)
	{
		return genericArgument != null
			? genericArgument.DeclaringType?.ToString() ?? genericArgument.DeclaringMethod.DeclaringType.ToString()
			: null;
	}
}
