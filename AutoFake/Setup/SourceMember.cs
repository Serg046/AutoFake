using System;
using System.Collections.Generic;
using System.Linq;
using AutoFake.Abstractions;
using Mono.Cecil;

namespace AutoFake.Setup
{
	internal class SourceMember
	{
		private readonly GenericArgument.Create _createGenericArgument;

		public SourceMember(ITypeInfo typeInfo, GenericArgument.Create createGenericArgument)
		{
			TypeInfo = typeInfo;
			_createGenericArgument = createGenericArgument;
		}

		public ITypeInfo TypeInfo { get; }

		public bool CompareGenericArguments(GenericParameter genericParameter, IEnumerable<GenericArgument> sourceArguments, IEnumerable<GenericArgument> stackArguments)
		{
			var source = sourceArguments.SingleOrDefault(a => a.Name == genericParameter.Name);
			var visited = stackArguments.FindGenericTypeOrDefault(genericParameter.Name);
			if (source == null || visited == null || source.Type != visited.Type)
			{
				return false;
			}

			return true;
		}

		public IEnumerable<GenericArgument> GetGenericArguments(Type[] genericArguments, Type[] genericParameters, string declaringType)
		{
			for (int i = 0; i < genericArguments.Length; i++)
			{
				var typeRef = TypeInfo.ImportToSourceAsm(genericArguments[i]);
				yield return _createGenericArgument(genericParameters[i].ToString(), typeRef.ToString(), declaringType);
			}
		}
	}
}
