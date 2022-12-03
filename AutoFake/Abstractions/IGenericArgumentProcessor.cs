using System.Collections.Generic;
using Mono.Cecil;

namespace AutoFake.Abstractions;

public interface IGenericArgumentProcessor
{
	IEnumerable<IGenericArgument> GetGenericArguments(MethodReference methodRef, MethodDefinition methodDef);
	IEnumerable<IGenericArgument> GetGenericArguments(FieldReference fieldRef);
}
