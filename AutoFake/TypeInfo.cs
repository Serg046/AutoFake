using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;

namespace AutoFake
{
    internal class TypeInfo
    {
        private const string FAKE_NAMESPACE = "AutoFake.Fakes";

        private AssemblyDefinition _assemblyDefinition;
        private TypeDefinition _typeDefinition;
        private MethodReference _addToListMethodInfo;

        public TypeInfo(Type sourceType, object[] contructorArgs)
        {
            SourceType = sourceType;
            ContructorArguments = contructorArgs;
        }

        public Type SourceType { get; }
        public object[] ContructorArguments { get; }
        
        public string FullTypeName => _typeDefinition.FullName;
        public MethodReference AddToListMethodInfo => _addToListMethodInfo;

        public void Load()
        {
            _assemblyDefinition = AssemblyDefinition.ReadAssembly(SourceType.Assembly.GetFiles().Single());

            _typeDefinition = _assemblyDefinition.MainModule.Types.Single(t => t.FullName == SourceType.FullName);
            _typeDefinition.Name = _typeDefinition.Name + "Fake";
            _typeDefinition.Namespace = FAKE_NAMESPACE;

            _addToListMethodInfo = Import(typeof(List<int>).GetMethod("Add"));
        }

        public TypeReference Import(Type type) => _assemblyDefinition.MainModule.Import(type);
        public MethodReference Import(MethodBase method) => _assemblyDefinition.MainModule.Import(method);

        public void AddField(FieldDefinition field) => _typeDefinition.Fields.Add(field);
        public void AddMethod(MethodDefinition method) => _typeDefinition.Methods.Add(method);

        public MethodDefinition SearchMethod(string methodName)
            => _typeDefinition.Methods.SingleOrDefault(m => m.Name == methodName);

        public void WriteAssembly(Stream stream)
        {
            _assemblyDefinition.Write(stream);
        }
    }
}
