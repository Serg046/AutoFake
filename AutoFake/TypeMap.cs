using System.Collections.Generic;
using AutoFake.Abstractions;
using Mono.Cecil;

namespace AutoFake
{
	internal class TypeMap : ITypeMap
	{
		private readonly ModuleDefinition _moduleDef;
		private readonly Dictionary<TypeDefinition, List<TypeDefinition>> _implementations;
		private readonly Dictionary<string, List<TypeDefinition>> _externalTypeImplementations;

		public TypeMap(ModuleDefinition moduleDef)
		{
			_moduleDef = moduleDef;
			_implementations = new Dictionary<TypeDefinition, List<TypeDefinition>>();
			_externalTypeImplementations = new Dictionary<string, List<TypeDefinition>>();
			Init();
		}

		private void Init()
		{
			foreach (var typeDef in GetAllTypes())
			{
				if (typeDef.BaseType != null)
				{
					AddImplementation(typeDef, typeDef.BaseType);
				}

				foreach (var interfaceDef in typeDef.Interfaces)
				{
					AddImplementation(typeDef, interfaceDef.InterfaceType);
				}
			}
		}

		private void AddImplementation(TypeDefinition currentTypeDef, TypeReference baseTypeRef)
		{
			if (baseTypeRef is TypeDefinition baseTypeDef && baseTypeDef.Scope == _moduleDef)
			{
				AddImplementation(_implementations, baseTypeDef, currentTypeDef);
			}
			else
			{
				AddImplementation(_externalTypeImplementations, baseTypeRef.GetElementType().FullName, currentTypeDef);
			}
		}

		private IEnumerable<TypeDefinition> GetAllTypes()
		{
			var types = new List<TypeDefinition>();
			foreach (var typeDefinition in _moduleDef.Types)
			{
				types.Add(typeDefinition);
				foreach (var nestedType in typeDefinition.NestedTypes)
				{
					AddNestedTypes(types, nestedType);
				}
			}
			return types;
		}

		private void AddNestedTypes(List<TypeDefinition> types, TypeDefinition type)
		{
			foreach (var nestedType in type.NestedTypes)
			{
				AddNestedTypes(types, nestedType);
			}
			types.Add(type);
		}

		private static void AddImplementation<T>(Dictionary<T, List<TypeDefinition>> dict,
			T type, TypeDefinition implementationDef) where T : class
		{
			if (!dict.ContainsKey(type)) dict[type] = new List<TypeDefinition>();
			dict[type].Add(implementationDef);
		}

		public ICollection<TypeDefinition> GetAllParentsAndDescendants(TypeDefinition typeDef)
		{
			var types = new HashSet<TypeDefinition>();
			GetAllParentsAndDescendants(typeDef, types);
			if (_externalTypeImplementations.TryGetValue(typeDef.GetElementType().FullName, out var implementations))
			{
				foreach (var implementation in implementations)
				{
					GetAllParentsAndDescendants(implementation, types);
				}
			}
			return types;
		}

		private void GetAllParentsAndDescendants(TypeDefinition typeDef, HashSet<TypeDefinition> types)
		{
			if (typeDef.Module != _moduleDef || !types.Add(typeDef)) return;

			if (typeDef.BaseType != null) GetAllParentsAndDescendants(typeDef.BaseType.ToTypeDefinition(), types);
			foreach (var interfaceDef in typeDef.Interfaces)
			{
				GetAllParentsAndDescendants(interfaceDef.InterfaceType.ToTypeDefinition(), types);
			}

			if (_implementations.TryGetValue(typeDef, out var implementations))
			{
				foreach (var implementation in implementations)
				{
					GetAllParentsAndDescendants(implementation, types);
				}
			}
		}
	}
}
