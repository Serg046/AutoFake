using System.Collections.Generic;
using Mono.Cecil;

namespace AutoFake
{
	internal class AssemblyDefinitionComparer : IEqualityComparer<AssemblyDefinition>
	{
		public bool Equals(AssemblyDefinition? x, AssemblyDefinition? y)
			=> x != null && y != null && x.FullName == y.FullName;

		public int GetHashCode(AssemblyDefinition obj) => obj.FullName.GetHashCode();
	}
}
