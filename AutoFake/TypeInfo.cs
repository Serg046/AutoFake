using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Cecil;

namespace AutoFake
{
    internal class TypeInfo : ITypeInfo
    {
        private readonly IAssemblyReader _assemblyReader;
        private readonly FakeOptions _fakeOptions;
        private readonly IAssemblyPool _assemblyPool;
        private readonly AssemblyNameReference _assemblyNameReference;

		public TypeInfo(IAssemblyReader assemblyReader, FakeOptions fakeOptions, IAssemblyPool assemblyPool,
	        Func<ModuleDefinition, ITypeMap> createTypeMap)
        {
            _assemblyReader = assemblyReader;
            _fakeOptions = fakeOptions;
            _assemblyPool = assemblyPool;
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
		        newType.DeclaringType = NewTypeReference(type);
		        newType = newType.DeclaringType;
	        }

	        TypeReference NewTypeReference(TypeReference typeRef)
		        => new(typeRef.Namespace, typeRef.Name, _assemblyReader.SourceTypeDefinition.Module, _assemblyNameReference, typeRef.IsValueType);

	        return result;
        }

        public TypeReference ImportToFieldsAsm(Type type)
	        => _assemblyReader.FieldsTypeDefinition.Module.ImportReference(type);

        public FieldReference ImportToFieldsAsm(FieldInfo field)
	        => _assemblyReader.FieldsTypeDefinition.Module.ImportReference(field);

        public MethodReference ImportToFieldsAsm(MethodBase method)
	        => _assemblyReader.FieldsTypeDefinition.Module.ImportReference(method);
	}
}
