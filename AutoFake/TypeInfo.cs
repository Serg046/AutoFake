using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using AutoFake.Setup;
using GuardExtensions;
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
        
        public string FullTypeName => _typeDefinition.FullName.Replace('/', '+');
        public MethodReference AddToListMethodInfo => _addToListMethodInfo;

        public void Load()
        {
            _assemblyDefinition = AssemblyDefinition.ReadAssembly(SourceType.Module.FullyQualifiedName);

            var type = _assemblyDefinition.MainModule.GetType(SourceType.FullName, true);
            _typeDefinition = type.Resolve();
            _typeDefinition.Name = _typeDefinition.Name + "Fake";
            _typeDefinition.Namespace = FAKE_NAMESPACE;

            _addToListMethodInfo = Import(typeof(List<int>).GetMethod("Add"));
        }

        public string GetInstalledMethodTypeName(FakeSetupPack setup)
        {
            Guard.IsNotNull(setup);

            string result = null;
            var type = setup.Method.DeclaringType;

            Func<string, string> combineFunc = typeName => result == null ? typeName : typeName + "/" + result;

            var tmpType = type;
            do
            {
                if (tmpType == SourceType)
                    return combineFunc(_typeDefinition.FullName);
                else
                    result = combineFunc(tmpType.Name);
            } while ((tmpType = tmpType.DeclaringType) != null);

            return type.Namespace + "." + result;
        }

        public TypeReference Import(Type type) => _assemblyDefinition.MainModule.Import(type);
        public MethodReference Import(MethodBase method) => _assemblyDefinition.MainModule.Import(method);

        public void AddField(FieldDefinition field) => _typeDefinition.Fields.Add(field);
        public void AddMethod(MethodDefinition method) => _typeDefinition.Methods.Add(method);

        public IEnumerable<FieldDefinition> Fields => _typeDefinition.Fields; 
        public IEnumerable<MethodDefinition> Methods => _typeDefinition.Methods;

        public void WriteAssembly(Stream stream)
        {
            _assemblyDefinition.Write(stream);
        }
    }
}
