using System.Collections.Generic;
using Mono.Cecil;

namespace AutoFake.Abstractions;

public interface ITypeMap
{
	ICollection<TypeDefinition> GetAllParentsAndDescendants(TypeDefinition typeDef);
}
