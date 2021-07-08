using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace AutoFake
{
	internal class TypeMap : ITypeMap
	{
		private readonly ModuleDefinition _moduleDef;
		private readonly Lazy<Dictionary<TypeDefinition, List<TypeDefinition>>> _implementations;

		public TypeMap(ModuleDefinition moduleDef)
		{
			_moduleDef = moduleDef;
			_implementations = new Lazy<Dictionary<TypeDefinition, List<TypeDefinition>>>(Init);
		}

		private Dictionary<TypeDefinition, List<TypeDefinition>> Init()
		{
			var dict = new Dictionary<TypeDefinition, List<TypeDefinition>>();
			foreach (var typeDef in GetAllTypes())
			{
				if (typeDef.BaseType != null && typeDef.BaseType.Scope == _moduleDef)
				{
					AddImplementation(dict, typeDef.BaseType.ToTypeDefinition(), typeDef);
				}

				foreach (var interfaceDef in typeDef.Interfaces.Where(intr => intr.InterfaceType.Module == _moduleDef))
				{
					AddImplementation(dict, interfaceDef.InterfaceType.ToTypeDefinition(), typeDef);
				}
			}
			return dict;
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

		private static void AddImplementation(Dictionary<TypeDefinition, List<TypeDefinition>> dict,
			TypeDefinition typeDef, TypeDefinition implementationDef)
		{
			if (!dict.ContainsKey(typeDef)) dict[typeDef] = new List<TypeDefinition>();
			dict[typeDef].Add(implementationDef);
		}

		public ICollection<TypeDefinition> GetAllParentsAndDescendants(TypeDefinition typeDef)
		{
			var types = new HashSet<TypeDefinition>();
			GetAllParentsAndDescendants(typeDef, types);
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

			if (_implementations.Value.TryGetValue(typeDef, out var implementations))
			{
				foreach (var implementation in implementations)
				{
					GetAllParentsAndDescendants(implementation, types);
				}
			}
		}
	}
}
