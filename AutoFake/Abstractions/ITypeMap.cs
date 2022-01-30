using System.Collections.Generic;
using Mono.Cecil;

namespace AutoFake.Abstractions
{
	internal interface ITypeMap
	{
		ICollection<TypeDefinition> GetAllParentsAndDescendants(TypeDefinition typeDef);
	}
}
