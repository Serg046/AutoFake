using System;
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;

namespace AutoFake
{
    internal interface ITypeInfo
    {
        Type SourceType { get; }
        string FullTypeName { get; }
        ModuleDefinition Module { get; }
        IEnumerable<FieldDefinition> Fields { get; }
        IEnumerable<MethodDefinition> Methods { get; }
        string GetMonoCecilTypeName(Type declaringType);
        void AddField(FieldDefinition field);
        void AddMethod(MethodDefinition method);
        void WriteAssembly(Stream stream);
        object CreateInstance(Type type);
        MethodReference ConvertToSourceAssembly(MethodReference constructor);
    }
}