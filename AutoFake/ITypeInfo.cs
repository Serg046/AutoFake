using System;
using System.Collections.Generic;
using System.IO;
using AutoFake.Setup;
using Mono.Cecil;

namespace AutoFake
{
    internal interface ITypeInfo
    {
        Type SourceType { get; }
        string FullTypeName { get; }
        ModuleDefinition Module { get; }
        ICollection<FieldDefinition> Fields { get; }
        ICollection<MethodDefinition> Methods { get; }
        void AddField(FieldDefinition field);
        void AddMethod(MethodDefinition method);
        void WriteAssembly(Stream stream);
        object CreateInstance(Type type);
        FakeObjectInfo CreateFakeObject(MockCollection mocks);
    }
}