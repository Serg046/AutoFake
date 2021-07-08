using System.Collections.Generic;
using Mono.Cecil;

namespace AutoFake
{
	internal interface ITypeMap
	{
		ICollection<TypeDefinition> GetAllParentsAndDescendants(TypeDefinition typeDef);
	}
}
