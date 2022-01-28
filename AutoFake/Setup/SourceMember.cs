using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace AutoFake.Setup
{
	internal class SourceMember
	{
		private readonly ITypeInfo _typeInfo;
		private readonly GenericArgument.Create _createGenericArgument;

		public SourceMember(ITypeInfo typeInfo, GenericArgument.Create createGenericArgument)
		{
			_typeInfo = typeInfo;
			_createGenericArgument = createGenericArgument;
		}

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

		protected IEnumerable<GenericArgument> GetGenericArguments(Type[] genericArguments, Type[] genericParameters, string declaringType)
		{
			for (int i = 0; i < genericArguments.Length; i++)
			{
				var typeRef = _typeInfo.ImportToSourceAsm(genericArguments[i]);
				yield return _createGenericArgument(genericParameters[i].ToString(), typeRef.ToString(), declaringType);
			}
		}
	}
}
