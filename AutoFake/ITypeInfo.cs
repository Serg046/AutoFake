using System;
using System.Collections.Generic;
using System.Reflection;
using AutoFake.Setup;
using Mono.Cecil;

namespace AutoFake
{
    internal interface ITypeInfo
    {
        IEnumerable<MethodDefinition> GetMethods(Predicate<MethodDefinition> methodPredicate);
        MethodDefinition GetMethod(MethodReference methodReference);
        void AddField(FieldDefinition field);
        FakeObjectInfo CreateFakeObject(MockCollection mocks);
        IList<MethodDefinition> GetDerivedVirtualMethods(MethodDefinition method);
        TypeReference ImportReference(Type type);
        FieldReference ImportReference(FieldInfo field);
        MethodReference ImportReference(MethodBase method);
    }
}