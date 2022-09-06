using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoFake.Abstractions;
using Mono.Cecil;

namespace AutoFake.Setup
{
	internal class SourceMember
	{
		private readonly GenericArgument.Create _createGenericArgument;

		public SourceMember(ITypeInfo typeInfo, GenericArgument.Create createGenericArgument, MemberInfo memberInfo)
		{
			TypeInfo = typeInfo;
			_createGenericArgument = createGenericArgument;
			DeclaringType = memberInfo.DeclaringType ?? throw new InvalidOperationException("Declaring type must be set");
			Name = memberInfo.Name;
		}

		public string Name { get; }

		protected Type DeclaringType { get; }

		protected ITypeInfo TypeInfo { get; }

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

		protected IEnumerable<GenericArgument> GetGenericArguments(Type type, string declaringType)
			=> GetGenericArguments(type.GetGenericArguments(), type.GetGenericTypeDefinition().GetGenericArguments(), declaringType);

		protected IEnumerable<GenericArgument> GetGenericArguments(MethodInfo method, string declaringType)
			=> GetGenericArguments(method.GetGenericArguments(), method.GetGenericMethodDefinition().GetGenericArguments(), declaringType);


		private IEnumerable<GenericArgument> GetGenericArguments(Type[] genericArguments, Type[] genericParameters, string declaringType)
		{
			for (int i = 0; i < genericArguments.Length; i++)
			{
				var typeRef = TypeInfo.ImportToSourceAsm(genericArguments[i]);
				yield return _createGenericArgument(genericParameters[i].ToString(), typeRef.ToString(), declaringType);
			}
		}
	}
}
