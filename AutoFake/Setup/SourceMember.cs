using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace AutoFake.Setup
{
	internal class SourceMember
	{
		protected bool CompareGenericArguments(GenericParameter genericParameter, IEnumerable<GenericArgument> sourceArguments, IEnumerable<GenericArgument> stackArguments)
		{
			var source = sourceArguments.SingleOrDefault(a => a.Name == genericParameter.Name);
			var visited = stackArguments.FindGenericTypeOrDefault(genericParameter.Name);
			if (source == null || visited == null || source.Type != visited.Type)
			{
				return false;
			}

			return true;
		}

		protected IEnumerable<GenericArgument> GetGenericArguments(IAssemblyWriter assemblyWriter, Type[] genericArguments, Type[] genericParameters, string declaringType)
		{
			for (int i = 0; i < genericArguments.Length; i++)
			{
				var typeRef = assemblyWriter.ImportToSourceAsm(genericArguments[i]);
				yield return new GenericArgument(genericParameters[i].ToString(), typeRef.ToString(), declaringType);
			}
		}
	}
}
