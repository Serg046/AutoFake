using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace AutoFake
{
    internal interface ITypeInfo
    {
        bool IsMultipleAssembliesMode { get; }
        Type SourceType { get; }
        ITypeMap TypeMap { get; }
        bool IsInFakeModule(TypeReference type);
        IEnumerable<MethodDefinition> GetMethods(Predicate<MethodDefinition> methodPredicate);
        MethodDefinition? GetMethod(MethodReference methodReference, bool searchInBaseType = false);
        MethodDefinition? GetMethod(TypeDefinition type, MethodReference methodReference, bool searchInBaseType = false);
        TypeDefinition GetTypeDefinition(Type type);
        ICollection<MethodDefinition> GetAllImplementations(MethodDefinition method, bool includeAffectedAssemblies = false);
        bool IsInReferencedAssembly(AssemblyDefinition assembly);
    }
}