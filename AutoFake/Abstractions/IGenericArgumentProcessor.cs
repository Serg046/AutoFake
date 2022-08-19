using System.Collections.Generic;
using Mono.Cecil;

namespace AutoFake.Abstractions
{
	internal interface IGenericArgumentProcessor
	{
		IEnumerable<GenericArgument> GetGenericArguments(MethodReference methodRef, MethodDefinition methodDef);
		IEnumerable<GenericArgument> GetGenericArguments(FieldReference fieldRef);
	}
}
