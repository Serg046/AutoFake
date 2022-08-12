using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoFake.Abstractions;
using Mono.Cecil;

namespace AutoFake
{
    internal class TypeInfo : ITypeInfo
    {
        private readonly IAssemblyReader _assemblyReader;
        private readonly IFakeOptions _fakeOptions;
        private readonly IAssemblyPool _assemblyPool;
        private readonly ICecilFactory _cecilFactory;
        private readonly AssemblyNameReference _assemblyNameReference;

		public TypeInfo(IAssemblyReader assemblyReader, IFakeOptions fakeOptions, IAssemblyPool assemblyPool,
	        ICecilFactory cecilFactory, Func<ModuleDefinition, ITypeMap> createTypeMap)
        {
            _assemblyReader = assemblyReader;
            _fakeOptions = fakeOptions;
            _assemblyPool = assemblyPool;
            _cecilFactory = cecilFactory;
            _assemblyNameReference = _assemblyReader.SourceTypeDefinition.Module.AssemblyReferences
	            .Single(a => a.FullName == _assemblyReader.SourceType.Assembly.FullName);

			TypeMap = createTypeMap(_assemblyReader.SourceTypeDefinition.Module);
        }
        
        public bool IsMultipleAssembliesMode
	        => _fakeOptions.AnalysisLevel == AnalysisLevels.AllExceptSystemAndMicrosoft || _fakeOptions.ReferencedTypes.Count > 0;

        public Type SourceType => _assemblyReader.SourceType;

        public ITypeMap TypeMap { get; }

        public bool IsInFakeModule(TypeReference type)
	        => !type.IsGenericParameter && type.Scope == _assemblyReader.SourceTypeDefinition.Module || type.GenericParameters.Any(t => t.Scope == _assemblyReader.SourceTypeDefinition.Module);

        public TypeDefinition GetTypeDefinition(Type type) =>
	        _assemblyReader.SourceTypeDefinition.Module.GetType(type.FullName, runtimeName: true).ToTypeDefinition();

        public FieldDefinition? GetField(Predicate<FieldDefinition> fieldPredicate)
	        => _assemblyReader.SourceTypeDefinition.Fields.SingleOrDefault(f => fieldPredicate(f));

        public IEnumerable<MethodDefinition> GetMethods(Predicate<MethodDefinition> methodPredicate) 
	        => _assemblyReader.SourceTypeDefinition.Methods.Where(m => methodPredicate(m));

        public MethodDefinition? GetMethod(MethodReference methodReference, bool searchInBaseType = false) =>
            GetMethod(_assemblyReader.SourceTypeDefinition, methodReference, searchInBaseType);

        public MethodDefinition? GetMethod(TypeDefinition type, MethodReference methodReference, bool searchInBaseType = false)
        {
	        var method = type.Methods.SingleOrDefault(m => m.EquivalentTo(methodReference));
	        if (searchInBaseType && method == null && type.BaseType != null)
	        {
		        return GetMethod(type.BaseType.ToTypeDefinition(), methodReference, searchInBaseType);
	        }

	        return method;
        }
        
        public ICollection<MethodDefinition> GetAllImplementations(MethodDefinition method, bool includeAffectedAssemblies = false)
        {
	        var methods = new HashSet<MethodDefinition>();
	        IEnumerable<ITypeMap> typeMaps = new[] {TypeMap};
	        if (includeAffectedAssemblies) typeMaps = typeMaps.Concat(_assemblyPool.GetTypeMaps());
	        foreach (var typeDef in typeMaps.SelectMany(m => m.GetAllParentsAndDescendants(method.DeclaringType)))
	        {
		        var methodDef = GetMethod(typeDef, method);
		        if (methodDef != null) methods.Add(methodDef);
            }

	        return methods;
        }

        public bool IsInReferencedAssembly(AssemblyDefinition assembly) => _assemblyPool.HasModule(assembly.MainModule);

        public TypeReference ImportToSourceAsm(Type type)
	        => _assemblyReader.SourceTypeDefinition.Module.ImportReference(type);

        public FieldReference ImportToSourceAsm(FieldInfo field)
	        => _assemblyReader.SourceTypeDefinition.Module.ImportReference(field);

        public MethodReference ImportToSourceAsm(MethodBase method)
	        => _assemblyReader.SourceTypeDefinition.Module.ImportReference(method);

		public TypeReference ImportToSourceAsm(TypeReference type)
		{
			if (type.IsGenericParameter) return type;

			var result = NewTypeReference(type);
			var newType = result;
			while (type.DeclaringType != null)
			{
				type = type.DeclaringType;
				newType.GetElementType().DeclaringType = NewTypeReference(type);
				newType = newType.DeclaringType;
			}

			TypeReference NewTypeReference(TypeReference typeRef)
			{
				var newTypeRef = _cecilFactory.CreateTypeReference(typeRef.Namespace, typeRef.Name, _assemblyReader.SourceTypeDefinition.Module, _assemblyNameReference, typeRef.IsValueType);
				if (typeRef is GenericInstanceType genericInstanceType)
				{
					var newGenericInstanceType = _cecilFactory.CreateGenericInstanceType(newTypeRef);
					foreach (var arg in genericInstanceType.GenericArguments)
					{
						newGenericInstanceType.GenericArguments.Add(arg);
					}

					return newGenericInstanceType;
				}

				return newTypeRef;
			}

			return result;
		}

		public MethodReference ImportToSourceAsm(MethodReference originalMethodRef)
		{
			var declaringType = ImportToSourceAsm(originalMethodRef.DeclaringType);
			var methodRef = _cecilFactory.CreateMethodReference(originalMethodRef.Name, originalMethodRef.ReturnType, declaringType);
			methodRef.CallingConvention = originalMethodRef.CallingConvention;
			methodRef.HasThis = originalMethodRef.HasThis;
			methodRef.ExplicitThis = originalMethodRef.ExplicitThis;

			foreach (var paramDef in originalMethodRef.Parameters)
			{
				methodRef.Parameters.Add(_cecilFactory.CreateParameterDefinition(paramDef.Name, paramDef.Attributes, paramDef.ParameterType));
			}

			return methodRef;
		}

		public TypeReference ImportToFieldsAsm(Type type)
	        => _assemblyReader.FieldsTypeDefinition.Module.ImportReference(type);

        public FieldReference ImportToFieldsAsm(FieldInfo field)
	        => _assemblyReader.FieldsTypeDefinition.Module.ImportReference(field);

        public MethodReference ImportToFieldsAsm(MethodBase method)
	        => _assemblyReader.FieldsTypeDefinition.Module.ImportReference(method);
	}
}
