using System;
using System.Collections.Generic;
using System.IO;
using AutoFake.Setup;
using Mono.Cecil;

namespace AutoFake
{
    internal interface ITypeInfo
    {
        ModuleDefinition Module { get; }
        IEnumerable<MethodDefinition> GetMethods(Predicate<MethodDefinition> methodPredicate);
        MethodDefinition GetMethod(MethodReference methodReference);
        void AddField(FieldDefinition field);
        void WriteAssembly(Stream stream);
        FakeObjectInfo CreateFakeObject(MockCollection mocks, FakeOptions options);
        IList<MethodDefinition> GetDerivedVirtualMethods(MethodDefinition method);
    }
}